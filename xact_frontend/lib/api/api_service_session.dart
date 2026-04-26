part of 'api_service.dart';

extension ApiServiceSessionMethods on ApiService {
  Future<GameSessionDetails> createLobby({required String lobbyName}) async {
    final hostUserId = await ensureMvpUser(
      preferredName: _session.currentUsername ?? 'Host',
      reuseByName: true,
    );
    await _closeOpenSessionsForHost(hostUserId);

    for (var attempt = 0; attempt < 3; attempt++) {
      final response = await _postJsonObject('/api/gamesessions', {
        'hostUserId': hostUserId,
        'sessionName': lobbyName,
        'joinCode': _generateJoinCode(),
        'status': 'WAITING',
        'plannedDurationMinutes': 60,
        'mrXRevealInterval': 5,
      });

      if (response != null) {
        final details = GameSessionDetails.fromJson(response);
        _session.setSession(
          sessionId: details.sessionId,
          joinCode: details.joinCode,
        );
        try {
          await _ensureRealtimeSubscription(details.sessionId);
        } catch (_) {
        }
        await _ensureStandardTeams(
          details.sessionId,
          hostUserId: hostUserId,
        );
        return details;
      }
    }

    throw Exception('Failed to create lobby after retries.');
  }

  Future<int> ensureMvpUser({
    required String preferredName,
    bool reuseByName = false,
  }) async {
    final users = await _listUsers();

    if (_session.currentUserId != null &&
        users.any((u) => u.userId == _session.currentUserId)) {
      return _session.currentUserId!;
    }

    if (reuseByName) {
      final preferredByName = users.where(
        (u) => u.username.toLowerCase() == preferredName.toLowerCase(),
      );
      if (preferredByName.isNotEmpty) {
        final user = preferredByName.first;
        _session.setIdentity(userId: user.userId, username: user.username);
        return user.userId;
      }
    }

    final desired = preferredName.trim().isEmpty
        ? 'Player'
        : preferredName.trim();
    final takenUsernames = users
        .map((u) => u.username.toLowerCase())
        .toSet();
    var candidate = desired;
    for (var i = 2; takenUsernames.contains(candidate.toLowerCase()); i++) {
      candidate = '$desired $i';
    }

    final emailLocal = candidate
        .toLowerCase()
        .replaceAll(RegExp(r'\s+'), '.');

    final created = await _postJsonObjectOrThrow('/api/users', {
      'username': candidate,
      'email': '$emailLocal@xact.local',
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
    } catch (_) {
    }
    await _ensureStandardTeams(session.sessionId);
    return session;
  }

  Future<void> ensureRealtimeSessionSubscription(int sessionId) async {
    await _ensureRealtimeSubscription(sessionId);
  }

  Future<void> _ensureStandardTeams(
    int sessionId, {
    int? hostUserId,
  }) async {
    final snapshot = await loadLobbySnapshot(sessionId);
    TeamDetails? misterXTeam;
    TeamDetails? detectiveTeam;

    for (final team in snapshot.teams) {
      if (team.role == TeamRole.mrX && misterXTeam == null) {
        misterXTeam = team;
      }
      if (team.role == TeamRole.detective && detectiveTeam == null) {
        detectiveTeam = team;
      }
    }

    if (misterXTeam == null) {
      misterXTeam = await addTeam(
        sessionId: sessionId,
        teamName: 'Mister X',
        role: TeamRole.mrX,
        colorCode: '#EF4444',
      );
    } else if (misterXTeam.teamName != 'Mister X') {
      await updateTeam(
        sessionId: sessionId,
        teamId: misterXTeam.teamId,
        teamName: 'Mister X',
        role: TeamRole.mrX,
        colorCode: misterXTeam.colorCode,
        isCaught: false,
        maxPlayerCount: misterXTeam.maxPlayerCount,
      );
    }

    if (detectiveTeam == null) {
      detectiveTeam = await addTeam(
        sessionId: sessionId,
        teamName: 'Detective 1',
        role: TeamRole.detective,
        colorCode: '#2563EB',
      );
    } else if (detectiveTeam.teamName != 'Detective 1') {
      await updateTeam(
        sessionId: sessionId,
        teamId: detectiveTeam.teamId,
        teamName: 'Detective 1',
        role: TeamRole.detective,
        colorCode: detectiveTeam.colorCode,
        isCaught: false,
        maxPlayerCount: detectiveTeam.maxPlayerCount,
      );
    }

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
      final details = infos
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
      membersByTeamId[team.teamId] = details;
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
    final json =
        await _postJsonObjectOrThrow('/api/gamesessions/$sessionId/teams', {
          'teamName': teamName,
          'role': _roleToApi(role),
          'colorCode': colorCode,
          'isCaught': false,
          'maxPlayerCount': maxPlayerCount,
        });

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

  Future<void> registerCurrentMemberPresence() async {
    final sessionId = _session.currentSessionId;
    final teamId = _session.currentTeamId;
    final memberId = _session.currentMemberId;
    if (sessionId == null || teamId == null || memberId == null) {
      return;
    }

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

  Future<TeamMemberDetails> addGuestMember({
    required int sessionId,
    required int teamId,
    required String guestName,
    bool isTeamLeader = false,
  }) async {
    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/teams/$teamId/members',
      {
        'userId': null,
        'guestName': guestName,
        'isTeamLeader': isTeamLeader,
        'currentLatitude': null,
        'currentLongitude': null,
        'lastUpdated': null,
      },
    );

    return TeamMemberDetails.fromJson(json);
  }

  Future<TeamMemberDetails> addUserMember({
    required int sessionId,
    required int teamId,
    required int userId,
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

  Future<TeamMemberDetails> moveMemberToTeam({
    required int sessionId,
    required TeamMemberDetails member,
    required int sourceTeamId,
    required int targetTeamId,
  }) async {
    if (sourceTeamId == targetTeamId) {
      return member;
    }

    await removeMember(
      sessionId: sessionId,
      teamId: sourceTeamId,
      memberId: member.memberId,
    );

    if (member.userId != null) {
      return addUserMember(
        sessionId: sessionId,
        teamId: targetTeamId,
        userId: member.userId!,
        isTeamLeader: member.isTeamLeader,
      );
    }

    return addGuestMember(
      sessionId: sessionId,
      teamId: targetTeamId,
      guestName: member.guestName ?? 'Guest',
      isTeamLeader: member.isTeamLeader,
    );
  }

  Future<void> startGameSession(int sessionId) async {
    await _postNoContent('/api/gamesessions/$sessionId/start');
  }

  Future<void> endGameSession(int sessionId) async {
    final details = await _getGameSession(sessionId);

    if (details.status == SessionStatus.finished) {
      return;
    }

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

  Future<void> closeCurrentSession() async {
    final sessionId = _session.currentSessionId;
    if (sessionId == null) {
      return;
    }

    final details = await _getGameSession(sessionId);
    final isHost =
        _session.currentUserId != null &&
        details.hostUserId == _session.currentUserId;

    if (isHost && details.status != SessionStatus.finished) {
      await _finishSession(details);
    }

    await _realtime.unsubscribeSession(sessionId);

    _session.currentSessionId = null;
    _session.currentJoinCode = null;
    _session.clearMembership();
  }

  Future<void> _closeOpenSessionsForHost(int hostUserId) async {
    final sessions = await _listGameSessions();

    for (final session in sessions) {
      if (session.status == SessionStatus.finished) {
        continue;
      }

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
      } catch (_) {
      }
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
      if (snapshot != null && snapshot.sessionId == sessionId) {
        return snapshot;
      }

      final latest = _realtime.latestSnapshot;
      if (latest != null && latest.sessionId == sessionId) {
        return latest;
      }
    } catch (_) {
    }

    return null;
  }

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
      for (final location in snapshot.latestLocations) location.memberId: location,
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

    Map<int, UserInfo> usersById = <int, UserInfo>{};
    try {
      final users = await _listUsers();
      usersById = {for (final user in users) user.userId: user};
    } catch (_) {
    }

    return LobbySnapshot(
      teams: teams,
      membersByTeamId: membersByTeamId,
      usersById: usersById,
      latestLocations: snapshot.latestLocations,
    );
  }

  Future<GeofencePointDetails> addGeofencePoint({
    required int sessionId,
    required double latitude,
    required double longitude,
    required int sequenceOrder,
  }) async {
    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/geofencepoints',
      {
        'latitude': latitude,
        'longitude': longitude,
        'sequenceOrder': sequenceOrder,
      },
    );

    return GeofencePointDetails.fromJson(json);
  }

  Future<void> deleteGeofencePoint({
    required int sessionId,
    required int pointId,
  }) async {
    await _deleteNoContent(
      '/api/gamesessions/$sessionId/geofencepoints/$pointId',
    );
  }

  Future<void> saveGeofenceArea({
    required int sessionId,
    required List<LatLng> points,
  }) async {
    final existing = await loadGeofencePoints(sessionId) ?? const [];
    await Future.wait(
      existing.map(
        (p) => deleteGeofencePoint(sessionId: sessionId, pointId: p.pointId),
      ),
    );

    for (var i = 0; i < points.length; i++) {
      await addGeofencePoint(
        sessionId: sessionId,
        latitude: points[i].latitude,
        longitude: points[i].longitude,
        sequenceOrder: i,
      );
    }
  }

  Future<int?> getActiveSessionId() async {
    if (_session.currentSessionId != null) {
      return _session.currentSessionId;
    }

    final sessions = await _listGameSessions();
    final active = sessions
        .where((s) => s.status == SessionStatus.active)
        .toList();
    if (active.isNotEmpty) {
      _session.currentSessionId = active.first.sessionId;
      _session.currentJoinCode = active.first.joinCode;
      return active.first.sessionId;
    }

    if (sessions.isEmpty) {
      return null;
    }

    _session.currentSessionId = sessions.first.sessionId;
    _session.currentJoinCode = sessions.first.joinCode;
    return sessions.first.sessionId;
  }
}