part of 'api_service.dart';

extension ApiServiceSessionMethods on ApiService {
  Future<GameSessionDetails> createLobby({required String lobbyName}) async {
    final hostUserId = await ensureMvpUser(
      preferredName: _session.currentUsername ?? 'Host',
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
        await _ensureStandardTeams(
          details.sessionId,
          hostUserId: hostUserId,
        );
        return details;
      }
    }

    throw Exception('Failed to create lobby after retries.');
  }

  Future<int> ensureMvpUser({required String preferredName}) async {
    final users = await _listUsers();

    if (_session.currentUserId != null &&
        users.any((u) => u.userId == _session.currentUserId)) {
      return _session.currentUserId!;
    }

    final preferredByName = users.where(
      (u) => u.username.toLowerCase() == preferredName.toLowerCase(),
    );
    if (preferredByName.isNotEmpty) {
      final user = preferredByName.first;
      _session.setIdentity(userId: user.userId, username: user.username);
      return user.userId;
    }

    try {
      final created = await _postJsonObjectOrThrow('/api/users', {
        'username': preferredName,
        'email': '${preferredName.toLowerCase()}@xact.local',
        'accountType': 'FREE',
        'subscriptionEndDate': null,
        'totalWins': 0,
        'totalGamesPlayed': 0,
      });

      final user = UserDetails.fromJson(created);
      _session.setIdentity(userId: user.userId, username: user.username);
      return user.userId;
    } catch (_) {
      if (users.isNotEmpty) {
        final user = users.first;
        _session.setIdentity(userId: user.userId, username: user.username);
        return user.userId;
      }
      rethrow;
    }
  }

  Future<GameSessionDetails> joinLobbyByCode(String joinCode) async {
    final json = await _getJsonObject('/api/gamesessions/join/$joinCode');
    final session = GameSessionDetails.fromJson(json);
    _session.setSession(
      sessionId: session.sessionId,
      joinCode: session.joinCode,
    );
    await _ensureStandardTeams(session.sessionId);
    return session;
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
    final teams = await _listTeams(sessionId);
    final users = await _listUsers();
    final usersById = {for (final user in users) user.userId: user};

    final membersByTeamId = <int, List<TeamMemberDetails>>{};
    for (final team in teams) {
      final infos = await _listTeamMembersByTeam(sessionId, team.teamId);
      final details = <TeamMemberDetails>[];
      for (final info in infos) {
        details.add(
          await _getTeamMemberById(sessionId, team.teamId, info.memberId),
        );
      }
      membersByTeamId[team.teamId] = details;
    }

    return LobbySnapshot(
      teams: teams,
      membersByTeamId: membersByTeamId,
      usersById: usersById,
    );
  }

  Future<TeamDetails> addTeam({
    required int sessionId,
    required String teamName,
    required TeamRole role,
    required String colorCode,
  }) async {
    final json =
        await _postJsonObjectOrThrow('/api/gamesessions/$sessionId/teams', {
          'teamName': teamName,
          'role': _roleToApi(role),
          'colorCode': colorCode,
          'isCaught': false,
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
  }) async {
    await _putJsonNoContent('/api/gamesessions/$sessionId/teams/$teamId', {
      'teamName': teamName,
      'role': _roleToApi(role),
      'colorCode': colorCode,
      'isCaught': isCaught,
    });
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
    await _postNoContent('/api/gamesessions/$sessionId/end');
  }

  Future<void> closeCurrentSession() async {
    final sessionId = _session.currentSessionId;
    if (sessionId == null) {
      return;
    }

    final details = await _getGameSession(sessionId);
    if (details.status == SessionStatus.finished) {
      return;
    }

    await _finishSession(details.sessionId);

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

        await _finishSession(details.sessionId);

        if (_session.currentSessionId == details.sessionId) {
          _session.currentSessionId = null;
          _session.currentJoinCode = null;
          _session.clearMembership();
        }
      } catch (_) {
      }
    }
  }

  Future<void> _finishSession(int sessionId) async {
    await endGameSession(sessionId);
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