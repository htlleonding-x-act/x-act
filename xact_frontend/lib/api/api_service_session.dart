part of 'api_service.dart';

const _defaultMrXTeamName = 'Team 1';
const _defaultDetectiveTeamName = 'Team 2';

extension ApiServiceSessionMethods on ApiService {
  Future<GameSessionDetails> createLobby({required String lobbyName}) async {
    final hostUserId = await ensureMvpUser(
      preferredName: _session.currentUsername ?? 'Host',
    );
    await _closeOpenSessionsForHost(hostUserId);

    // only a clash of the random join code is worth another attempt, any
    // other rejection fails the same way again
    for (var attempt = 0; attempt < 3; attempt++) {
      final Map<String, dynamic> response;
      try {
        response = await _postJsonObjectOrThrow('/api/gamesessions', {
          'hostUserId': hostUserId,
          'sessionName': lobbyName,
          'joinCode': _generateJoinCode(),
          'status': 'WAITING',
          'plannedDurationMinutes': 60,
          'mrXRevealInterval': 5,
        });
      } on ApiException catch (e) {
        if (e.code == 'join_code_in_use') continue;
        rethrow;
      }

      final details = GameSessionDetails.fromJson(response);
      _session.setSession(
        sessionId: details.sessionId,
        joinCode: details.joinCode,
      );
      try {
        await _ensureRealtimeSubscription(details.sessionId);
      } catch (_) {}
      await _ensureStandardTeams(details.sessionId, hostUserId: hostUserId);
      return details;
    }

    throw Exception('Failed to create lobby after retries.');
  }

  /// copies the teams, players, area and settings of a finished session into a
  /// new lobby. the backend then sends `rematch_created` on the finished
  /// session's channel so every connected client moves over
  Future<GameSessionDetails> createRematch(int finishedSessionId) async {
    for (var attempt = 0; attempt < 3; attempt++) {
      final response = await _postJsonObject(
        '/api/gamesessions/$finishedSessionId/rematch',
        {'joinCode': _generateJoinCode()},
      );

      if (response != null) {
        return GameSessionDetails.fromJson(response);
      }
    }

    throw Exception('Failed to create rematch after retries.');
  }

  Future<String> ensureMvpUser({required String preferredName}) async {
    if (isAuthenticated) {
      try {
        await _syncUserWithBackend();
        if (_session.currentUserId != null) return _session.currentUserId!;
      } catch (_) {}
    }

    return _createGuestUser(preferredName: preferredName);
  }

  Future<void> _syncUserWithBackend() async {
    final json = await _postJsonObjectOrThrow('/api/auth/register', {});
    final user = UserDetails.fromJson(json);
    _session.setIdentity(userId: user.userId, username: user.username);
    try {
      await AuthStorage.saveIdentity(
        userId: user.userId,
        username: user.username,
      );
    } catch (_) {}
  }

  /// guest names only have to be unique within a session, so a new name just
  /// creates a new guest instead of renaming the current one
  Future<String> _createGuestUser({required String preferredName}) async {
    final name = preferredName.trim().isEmpty ? 'Player' : preferredName.trim();

    if (_session.currentUserId != null && _session.currentUsername == name) {
      return _session.currentUserId!;
    }

    final created = await _postJsonObjectOrThrow('/api/users', {
      'username': name,
      'accountType': 'FREE',
      'subscriptionEndDate': null,
      'totalWins': 0,
      'totalGamesPlayed': 0,
    });

    final user = UserDetails.fromJson(created);
    _session.setIdentity(userId: user.userId, username: user.username);
    return user.userId;
  }

  Future<GameSessionDetails> joinLobbyByCode(String joinCode) async {
    final json = await _getJsonObject('/api/gamesessions/join/$joinCode');
    final session = GameSessionDetails.fromJson(json);
    _session.setSession(
      sessionId: session.sessionId,
      joinCode: session.joinCode,
    );
    try {
      await _ensureRealtimeSubscription(session.sessionId);
    } catch (_) {}
    return session;
  }

  Future<void> ensureRealtimeSessionSubscription(int sessionId) async {
    await _ensureRealtimeSubscription(sessionId);
  }

  Future<void> _ensureStandardTeams(
    int sessionId, {
    String? hostUserId,
  }) async {
    final snapshot = await loadLobbySnapshot(sessionId);
    TeamDetails? misterXTeam;
    TeamDetails? detectiveTeam;

    for (final team in snapshot.teams) {
      if (team.role == TeamRole.mrX) misterXTeam ??= team;
      if (team.role == TeamRole.detective) detectiveTeam ??= team;
    }

    misterXTeam ??= await addTeam(
      sessionId: sessionId,
      teamName: _defaultMrXTeamName,
      role: TeamRole.mrX,
      colorCode: '#EF4444',
    );

    detectiveTeam ??= await addTeam(
      sessionId: sessionId,
      teamName: _defaultDetectiveTeamName,
      role: TeamRole.detective,
      colorCode: '#2563EB',
    );

    if (hostUserId != null) {
      final hostAlreadyAssigned = snapshot.membersByTeamId.values
          .expand((members) => members)
          .any((member) => member.userId == hostUserId);

      if (!hostAlreadyAssigned) {
        await addUserMember(
          sessionId: sessionId,
          teamId: detectiveTeam.teamId,
          userId: hostUserId,
          isTeamLeader: true,
        );
      }
    }
  }

  Future<LobbySnapshot> loadLobbySnapshot(int sessionId) async {
    final realtimeSnapshot = await _tryLoadRealtimeSnapshot(sessionId);
    if (realtimeSnapshot != null) {
      return _toLobbySnapshot(realtimeSnapshot);
    }

    final teams = await _listTeams(sessionId);
    final users = await _listUsers();
    final usersById = {for (final user in users) user.userId: user};

    final membersByTeamId = <int, List<TeamMemberDetails>>{};
    for (final team in teams) {
      final infos = await _listTeamMembersByTeam(sessionId, team.teamId);
      membersByTeamId[team.teamId] = infos
          .map(
            (info) => TeamMemberDetails(
              memberId: info.memberId,
              teamId: info.teamId,
              sessionId: info.sessionId,
              userId: info.userId,
              guestName: info.guestName,
              isTeamLeader: info.isTeamLeader,
              currentLatitude: null,
              currentLongitude: null,
              lastUpdated: null,
            ),
          )
          .toList(growable: false);
    }

    return LobbySnapshot(
      teams: teams,
      membersByTeamId: membersByTeamId,
      usersById: usersById,
      latestLocations: const [],
    );
  }

  Future<TeamDetails> addTeam({
    required int sessionId,
    required String teamName,
    required TeamRole role,
    required String colorCode,
    int maxPlayerCount = 6,
  }) async {
    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/teams',
      {
        'teamName': teamName,
        'role': _roleToApi(role),
        'colorCode': colorCode,
        'isCaught': false,
        'maxPlayerCount': maxPlayerCount,
      },
    );
    return TeamDetails.fromJson(json);
  }

  Future<void> updateTeam({
    required int sessionId,
    required int teamId,
    required String teamName,
    required TeamRole role,
    required String colorCode,
    required bool isCaught,
    required int maxPlayerCount,
  }) async {
    await _putJsonNoContent('/api/gamesessions/$sessionId/teams/$teamId', {
      'teamName': teamName,
      'role': _roleToApi(role),
      'colorCode': colorCode,
      'isCaught': isCaught,
      'maxPlayerCount': maxPlayerCount,
    });
  }

  /// the backend swaps the roles of both teams. the new roles arrive through
  /// the realtime `team_updated` and `mr_x_caught` events
  Future<void> markMrXCaught({
    required int sessionId,
    required int catchingTeamId,
  }) async {
    await _postJsonNoContent('/api/gamesessions/$sessionId/catch', {
      'catchingTeamId': catchingTeamId,
    });
  }

  Future<void> registerCurrentMemberPresence() async {
    final sessionId = _session.currentSessionId;
    final teamId = _session.currentTeamId;
    final memberId = _session.currentMemberId;
    if (sessionId == null || teamId == null || memberId == null) return;

    await _realtime.registerMemberPresence(
      sessionId: sessionId,
      teamId: teamId,
      memberId: memberId,
      userId: _session.currentUserId,
      guestName: _session.currentUserId == null ? _session.currentUsername : null,
    );
  }

  Future<void> unregisterCurrentMemberPresence() async {
    await _realtime.unregisterMemberPresence();
  }

  Future<void> deleteTeam({required int sessionId, required int teamId}) async {
    await _deleteNoContent('/api/gamesessions/$sessionId/teams/$teamId');
  }

  Future<TeamMemberDetails> addUserMember({
    required int sessionId,
    required int teamId,
    required String userId,
    bool isTeamLeader = false,
  }) async {
    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/teams/$teamId/members',
      {
        'userId': userId,
        'guestName': null,
        'isTeamLeader': isTeamLeader,
        'currentLatitude': null,
        'currentLongitude': null,
        'lastUpdated': null,
      },
    );
    return TeamMemberDetails.fromJson(json);
  }

  Future<void> removeMember({
    required int sessionId,
    required int teamId,
    required int memberId,
  }) async {
    await _deleteNoContent(
      '/api/gamesessions/$sessionId/teams/$teamId/members/$memberId',
    );
  }

  Future<void> moveMemberToTeam({
    required int sessionId,
    required TeamMemberDetails member,
    required int sourceTeamId,
    required int targetTeamId,
  }) async {
    if (sourceTeamId == targetTeamId) {
      return;
    }

    await _putJsonNoContent(
      '/api/gamesessions/$sessionId/teams/$sourceTeamId/members/${member.memberId}',
      {
        'userId': member.userId,
        'guestName': member.userId == null ? (member.guestName ?? 'Guest') : null,
        'isTeamLeader': member.isTeamLeader,
        'currentLatitude': null,
        'currentLongitude': null,
        'lastUpdated': null,
        'teamId': targetTeamId,
      },
    );
  }

  Future<void> startGameSession(int sessionId) async {
    await _postNoContent('/api/gamesessions/$sessionId/start');
  }

  Future<void> endGameSession(int sessionId) async {
    final details = await _getGameSession(sessionId);

    if (details.status == SessionStatus.finished) return;

    if (details.status == SessionStatus.active) {
      await _postNoContent('/api/gamesessions/$sessionId/end');
      return;
    }

    if (details.status == SessionStatus.waiting) {
      await _deleteNoContent('/api/gamesessions/$sessionId');
      return;
    }

    throw StateError('Unsupported session state transition for ending.');
  }

  /// remembers the running match so the player can get back in after the app
  /// was closed or killed
  Future<void> rememberActiveGame() async {
    final sessionId = _session.currentSessionId;
    final joinCode = _session.currentJoinCode;
    final teamId = _session.currentTeamId;
    final memberId = _session.currentMemberId;
    final userId = _session.currentUserId;
    final username = _session.currentUsername;
    if (sessionId == null ||
        joinCode == null ||
        teamId == null ||
        memberId == null ||
        userId == null ||
        username == null) {
      return;
    }

    try {
      await ActiveGameStorage.save((
        sessionId: sessionId,
        joinCode: joinCode,
        teamId: teamId,
        memberId: memberId,
        isTeamLeader: _session.isTeamLeader,
        userId: userId,
        username: username,
      ));
    } catch (_) {}
  }

  /// the remembered match, if it still runs and this player is still in it
  Future<({ActiveGame game, String sessionName})?> loadResumableGame() async {
    final ActiveGame? game;
    try {
      game = await ActiveGameStorage.load();
    } catch (_) {
      return null;
    }
    if (game == null) return null;

    // a different account signed in since then
    if (isAuthenticated && _session.currentUserId != game.userId) {
      await _forgetActiveGame();
      return null;
    }

    try {
      final details = await _getGameSession(game.sessionId);
      final members = await _listTeamMembersByTeam(game.sessionId, game.teamId);
      if (details.status != SessionStatus.active ||
          !members.any((m) => m.memberId == game!.memberId)) {
        await _forgetActiveGame();
        return null;
      }
      return (game: game, sessionName: details.sessionName);
    } on ApiException catch (e) {
      if (e.statusCode == 404) await _forgetActiveGame();
      return null;
    } catch (_) {
      // offline, keep it for the next try
      return null;
    }
  }

  Future<void> resumeGame(ActiveGame game) async {
    if (!isAuthenticated) {
      _session.setIdentity(userId: game.userId, username: game.username);
    }
    _session.setSession(sessionId: game.sessionId, joinCode: game.joinCode);
    _session.setMembership(
      teamId: game.teamId,
      memberId: game.memberId,
      teamLeader: game.isTeamLeader,
    );

    try {
      await _ensureRealtimeSubscription(game.sessionId);
      await registerCurrentMemberPresence();
    } catch (_) {
      // the game screen retries realtime on its own
    }
  }

  Future<void> _forgetActiveGame() async {
    try {
      await ActiveGameStorage.clear();
    } catch (_) {}
  }

  Future<void> closeCurrentSession() async {
    final sessionId = _session.currentSessionId;
    if (sessionId == null) return;

    final details = await _getGameSession(sessionId);
    final isHost =
        _session.currentUserId != null &&
        details.hostUserId == _session.currentUserId;

    final teamId = _session.currentTeamId;
    final memberId = _session.currentMemberId;
    if (isHost && details.status != SessionStatus.finished) {
      await _finishSession(details);
    } else if (details.status != SessionStatus.finished &&
        teamId != null &&
        memberId != null) {
      // delete the member like a kick does, otherwise they stay in the lobby or
      // on everyone's map and still count in kick votes. the presence is dropped
      // below, so the lobby disconnect cleanup won't remove them either. errors
      // are ignored so leaving also works offline
      try {
        await removeMember(
          sessionId: sessionId,
          teamId: teamId,
          memberId: memberId,
        );
      } catch (_) {}
    }

    // drop the realtime presence first so a rematch started after this point,
    // e.g. from the end match screen, doesn't copy this player into the new
    // lobby as a ghost
    try {
      await _realtime.unregisterMemberPresence();
    } catch (_) {}
    await _realtime.unsubscribeSession(sessionId);
    await _forgetActiveGame();

    _session.currentSessionId = null;
    _session.currentJoinCode = null;
    _session.clearMembership();
  }

  /// leaves without ending the session for everyone else. used after a kick:
  /// the server already removed the member, so only the realtime presence and
  /// the local session state get cleared
  Future<void> leaveCurrentSessionLocally() async {
    final sessionId = _session.currentSessionId;
    if (sessionId == null) {
      return;
    }

    try {
      await _realtime.unregisterMemberPresence();
    } catch (_) {}
    try {
      await _realtime.unsubscribeSession(sessionId);
    } catch (_) {}
    await _forgetActiveGame();

    _session.currentSessionId = null;
    _session.currentJoinCode = null;
    _session.clearMembership();
  }

  Future<void> _closeOpenSessionsForHost(String hostUserId) async {
    final sessions = await _listGameSessions();

    for (final session in sessions) {
      if (session.status == SessionStatus.finished) continue;

      try {
        final details = await _getGameSession(session.sessionId);
        if (details.hostUserId != hostUserId ||
            details.status == SessionStatus.finished) {
          continue;
        }

        await _finishSession(details);

        if (_session.currentSessionId == details.sessionId) {
          _session.currentSessionId = null;
          _session.currentJoinCode = null;
          _session.clearMembership();
        }
      } catch (_) {}
    }
  }

  Future<void> _finishSession(GameSessionDetails details) async {
    if (details.status == SessionStatus.active) {
      await _postNoContent('/api/gamesessions/${details.sessionId}/end');
      return;
    }

    if (details.status == SessionStatus.waiting) {
      await _deleteNoContent('/api/gamesessions/${details.sessionId}');
    }
  }

  Future<void> _ensureRealtimeSubscription(int sessionId) async {
    await _realtime.connect(baseUrl: _baseUri.toString());
    await _realtime.subscribeSession(sessionId);
  }

  Future<GameSessionSnapshot?> _tryLoadRealtimeSnapshot(int sessionId) async {
    try {
      await _ensureRealtimeSubscription(sessionId);
      final snapshot = await _realtime.requestSnapshot(sessionId);
      if (snapshot != null && snapshot.sessionId == sessionId) return snapshot;

      final latest = _realtime.latestSnapshot;
      if (latest != null && latest.sessionId == sessionId) return latest;
    } catch (_) {}

    return null;
  }

  Future<LobbySnapshot> toLobbySnapshot(GameSessionSnapshot snapshot) =>
      _toLobbySnapshot(snapshot);

  Future<LobbySnapshot> _toLobbySnapshot(GameSessionSnapshot snapshot) async {
    final teams = snapshot.teams
        .map(
          (team) => TeamDetails(
            teamId: team.id,
            sessionId: team.sessionId,
            teamName: team.teamName,
            role: team.role,
            colorCode: team.colorCode,
            isCaught: team.isCaught,
            maxPlayerCount: team.maxPlayerCount,
          ),
        )
        .toList(growable: false);

    final latestByMemberId = {
      for (final loc in snapshot.latestLocations) loc.memberId: loc,
    };

    final membersByTeamId = <int, List<TeamMemberDetails>>{};
    for (final member in snapshot.members) {
      final location = latestByMemberId[member.id];
      final details = TeamMemberDetails(
        memberId: member.id,
        teamId: member.teamId,
        sessionId: member.sessionId,
        userId: member.userId,
        guestName: member.guestName,
        isTeamLeader: member.isTeamLeader,
        currentLatitude: location?.latitude ?? member.currentLatitude,
        currentLongitude: location?.longitude ?? member.currentLongitude,
        lastUpdated: location?.timestamp ?? member.lastUpdated,
      );
      membersByTeamId.putIfAbsent(member.teamId, () => <TeamMemberDetails>[]);
      membersByTeamId[member.teamId]!.add(details);
    }

    // usernames don't change during a session and this runs on every location
    // ping, so only refetch users when an unknown user id shows up
    final hasUnknownUser = snapshot.members.any(
      (member) =>
          member.userId != null && !_usersById.containsKey(member.userId),
    );
    if (hasUnknownUser) {
      try {
        final users = await _listUsers();
        _usersById
          ..clear()
          ..addAll({for (final user in users) user.userId: user});
      } catch (_) {
      }
    }

    return LobbySnapshot(
      teams: teams,
      membersByTeamId: membersByTeamId,
      usersById: Map.of(_usersById),
      latestLocations: snapshot.latestLocations,
    );
  }

  Future<GameSessionDetails> getGameSession(int sessionId) async =>
      _getGameSession(sessionId);

  Future<void> updateSessionPingInterval({
    required int sessionId,
    required int mrXRevealInterval,
  }) async {
    String statusString(SessionStatus? s) => switch (s) {
      SessionStatus.active => 'ACTIVE',
      SessionStatus.finished => 'FINISHED',
      _ => 'WAITING',
    };

    final details = await _getGameSession(sessionId);
    await _putJsonNoContent('/api/gamesessions/$sessionId', {
      'hostUserId': details.hostUserId,
      'sessionName': details.sessionName,
      'joinCode': details.joinCode,
      'status': statusString(details.status),
      'startTime': null,
      'endTime': null,
      'plannedDurationMinutes': details.plannedDurationMinutes,
      'mrXRevealInterval': mrXRevealInterval,
    });
  }

  Future<void> saveGeofenceArea({
    required int sessionId,
    required List<LatLng> points,
  }) async {
    // one request, so a dropped connection can't leave half an area behind
    await _putJsonNoContent('/api/gamesessions/$sessionId/geofencepoints', {
      'points': [
        for (final p in points)
          {'latitude': p.latitude, 'longitude': p.longitude},
      ],
    });
  }

  Future<int?> getActiveSessionId() async {
    if (_session.currentSessionId != null) return _session.currentSessionId;

    final sessions = await _listGameSessions();
    final active = sessions.where((s) => s.status == SessionStatus.active).toList();
    if (active.isNotEmpty) {
      _session.currentSessionId = active.first.sessionId;
      _session.currentJoinCode = active.first.joinCode;
      return active.first.sessionId;
    }

    if (sessions.isEmpty) return null;

    _session.currentSessionId = sessions.first.sessionId;
    _session.currentJoinCode = sessions.first.joinCode;
    return sessions.first.sessionId;
  }
}
