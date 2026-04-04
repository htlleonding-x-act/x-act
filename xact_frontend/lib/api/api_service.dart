import 'dart:convert';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';

import '../services/app_session.dart';
import 'api_config.dart';
import 'models.dart';

final class TeamCardData {
  final String teamName;
  final Color color;
  final bool isMisterX;
  final List<String> members;

  const TeamCardData({
    required this.teamName,
    required this.color,
    required this.isMisterX,
    required this.members,
  });
}

final class TeamChatHeaderData {
  final String teamName;
  final int memberCount;
  final Color teamColor;

  const TeamChatHeaderData({
    required this.teamName,
    required this.memberCount,
    required this.teamColor,
  });
}

final class MapHeaderData {
  final String nextPingText;
  final int remainingMinutes;
  final int intervalMinutes;

  double get progress =>
      intervalMinutes > 0 ? 1.0 - (remainingMinutes / intervalMinutes) : 0.0;

  const MapHeaderData({
    required this.nextPingText,
    this.remainingMinutes = 0,
    this.intervalMinutes = 0,
  });
}

final class PlayerPositionData {
  final int memberId;
  final int teamId;
  final String displayName;
  final TeamRole? teamRole;
  final Color color;
  final LatLng position;

  const PlayerPositionData({
    required this.memberId,
    required this.teamId,
    required this.displayName,
    required this.teamRole,
    required this.color,
    required this.position,
  });
}

final class LobbySnapshot {
  final List<TeamDetails> teams;
  final Map<int, List<TeamMemberDetails>> membersByTeamId;
  final Map<int, UserInfo> usersById;

  const LobbySnapshot({
    required this.teams,
    required this.membersByTeamId,
    required this.usersById,
  });
}

final class ApiService {
  ApiService._({required String baseUrl})
    : _baseUri = Uri.parse(baseUrl),
      _http = http.Client();

  static final ApiService instance = ApiService._(baseUrl: ApiConfig.baseUrl);

  final Uri _baseUri;
  final http.Client _http;
  final AppSession _session = AppSession.instance;

  Future<List<TeamCardData>> loadTeamCards() async {
    final sessionId = await getActiveSessionId();
    if (sessionId == null) {
      return const [];
    }

    final snapshot = await loadLobbySnapshot(sessionId);

    return snapshot.teams
        .map((team) {
          final color = tryParseHexColor(team.colorCode) ?? Colors.white;
          final members = (snapshot.membersByTeamId[team.teamId] ?? const [])
              .map((member) {
                if (member.userId != null) {
                  return snapshot.usersById[member.userId!]?.username ??
                      'User ${member.userId}';
                }
                return member.guestName ?? 'Guest';
              })
              .toList(growable: false);

          return TeamCardData(
            teamName: team.teamName,
            color: color,
            isMisterX: team.role == TeamRole.mrX,
            members: members,
          );
        })
        .toList(growable: false);
  }

  Future<TeamChatHeaderData> loadTeamChatHeader() async {
    final sessionId = await getActiveSessionId();
    if (sessionId == null) {
      throw StateError('No active session found.');
    }

    final snapshot = await loadLobbySnapshot(sessionId);
    final team = snapshot.teams.firstWhere(
      (t) => t.teamId == _session.currentTeamId,
      orElse: () => snapshot.teams.first,
    );

    final memberCount =
        (snapshot.membersByTeamId[team.teamId] ?? const []).length;

    return TeamChatHeaderData(
      teamName: team.teamName,
      memberCount: memberCount,
      teamColor: tryParseHexColor(team.colorCode) ?? Colors.blue,
    );
  }

  Future<MapHeaderData> loadMapHeader() async {
    final sessionId = await getActiveSessionId();
    if (sessionId == null) {
      return const MapHeaderData(nextPingText: 'Next ping: -');
    }

    final details = await _getGameSession(sessionId);
    final start = details.startTime;
    final interval = details.mrXRevealInterval;

    if (start == null || interval <= 0) {
      return const MapHeaderData(nextPingText: 'Next ping: -');
    }

    final now = DateTime.now().toUtc();
    final elapsedMinutes = now.difference(start.toUtc()).inMinutes;
    final minutesIntoCycle = elapsedMinutes % interval;
    final remaining = (interval - minutesIntoCycle) % interval;

    final display = remaining == 0 ? interval : remaining;
    return MapHeaderData(
      nextPingText: 'Next ping: ${display}m',
      remainingMinutes: display,
      intervalMinutes: interval,
    );
  }

  Future<GameSessionDetails> createLobby({required String lobbyName}) async {
    final hostUserId = await ensureMvpUser(
      preferredName: _session.currentUsername ?? 'Host',
    );

    // Retry a few times to avoid accidental join-code collisions.
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
    return session;
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

  Future<List<PlayerPositionData>> loadPlayerPositions(int sessionId) async {
    final snapshot = await loadLobbySnapshot(sessionId);
    final out = <PlayerPositionData>[];

    for (final team in snapshot.teams) {
      final color = tryParseHexColor(team.colorCode) ?? Colors.blueGrey;
      final members = snapshot.membersByTeamId[team.teamId] ?? const [];
      for (final member in members) {
        final lat = member.currentLatitude;
        final lon = member.currentLongitude;
        if (lat == null || lon == null) {
          continue;
        }

        final name = member.userId != null
            ? (snapshot.usersById[member.userId!]?.username ??
                  'User ${member.userId}')
            : (member.guestName ?? 'Guest');

        out.add(
          PlayerPositionData(
            memberId: member.memberId,
            teamId: team.teamId,
            displayName: name,
            teamRole: team.role,
            color: color,
            position: LatLng(lat, lon),
          ),
        );
      }
    }

    return out;
  }

  Future<void> updateTeamMemberLocation({
    required int sessionId,
    required int memberId,
    required int teamId,
    required int userId,
    required bool isTeamLeader,
    required double latitude,
    required double longitude,
  }) async {
    await _putJsonNoContent(
      '/api/gamesessions/$sessionId/teams/$teamId/members/$memberId',
      {
        'userId': userId,
        'guestName': null,
        'isTeamLeader': isTeamLeader,
        'currentLatitude': latitude,
        'currentLongitude': longitude,
        'lastUpdated': DateTime.now().toUtc().toIso8601String(),
      },
    );
  }

  Future<List<GeofencePointDetails>> loadGeofencePoints(int sessionId) async {
    final json = await _getJsonObject(
      '/api/gamesessions/$sessionId/geofencepoints',
    );
    final infos = ApiListResponse.fromJson(
      json,
      GeofencePointInfo.fromJson,
    ).items;

    final details = await Future.wait(
      infos.map((p) async {
        final point = await _getJsonObject(
          '/api/gamesessions/$sessionId/geofencepoints/${p.pointId}',
        );
        return GeofencePointDetails.fromJson(point);
      }),
    );

    return details..sort((a, b) => a.sequenceOrder.compareTo(b.sequenceOrder));
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
    final existing = await loadGeofencePoints(sessionId);
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

  Future<List<UserInfo>> _listUsers() async {
    final json = await _getJsonObject('/api/users');
    return ApiListResponse.fromJson(json, UserInfo.fromJson).items;
  }

  Future<List<GameSessionInfo>> _listGameSessions() async {
    final json = await _getJsonObject('/api/gamesessions');
    return ApiListResponse.fromJson(json, GameSessionInfo.fromJson).items;
  }

  Future<GameSessionDetails> _getGameSession(int sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId');
    return GameSessionDetails.fromJson(json);
  }

  Future<List<TeamDetails>> _listTeams(int sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId/teams');
    final items = ApiListResponse.fromJson(json, TeamInfo.fromJson).items;

    final details = await Future.wait(
      items.map((team) => _getTeam(sessionId, team.teamId)),
    );
    return details;
  }

  Future<TeamDetails> _getTeam(int sessionId, int teamId) async {
    final json = await _getJsonObject(
      '/api/gamesessions/$sessionId/teams/$teamId',
    );
    return TeamDetails.fromJson(json);
  }

  Future<List<TeamMemberInfo>> _listTeamMembersByTeam(
    int sessionId,
    int teamId,
  ) async {
    final json = await _getJsonObject(
      '/api/gamesessions/$sessionId/teams/$teamId/members',
    );
    return ApiListResponse.fromJson(json, TeamMemberInfo.fromJson).items;
  }

  Future<TeamMemberDetails> _getTeamMemberById(
    int sessionId,
    int teamId,
    int memberId,
  ) async {
    final json = await _getJsonObject(
      '/api/gamesessions/$sessionId/teams/$teamId/members/$memberId',
    );
    return TeamMemberDetails.fromJson(json);
  }

  Future<Map<String, dynamic>> _getJsonObject(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.get(
      uri,
      headers: {'Accept': 'application/json'},
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for GET $path');
    }

    final decoded = jsonDecode(response.body);
    if (decoded is Map<String, dynamic>) {
      return decoded;
    }

    throw const FormatException('Expected JSON object response.');
  }

  Future<Map<String, dynamic>?> _postJsonObject(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.post(
      uri,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
      body: jsonEncode(payload),
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) {
        return <String, dynamic>{};
      }
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic>) {
        return decoded;
      }
      throw const FormatException('Expected JSON object response.');
    }

    return null;
  }

  Future<Map<String, dynamic>> _postJsonObjectOrThrow(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final json = await _postJsonObject(path, payload);
    if (json == null) {
      throw Exception('POST $path failed');
    }
    return json;
  }

  Future<void> _postNoContent(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.post(
      uri,
      headers: {'Accept': 'application/json'},
    );
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for POST $path');
    }
  }

  Future<void> _putJsonNoContent(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.put(
      uri,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
      body: jsonEncode(payload),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for PUT $path');
    }
  }

  Future<void> _deleteNoContent(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.delete(
      uri,
      headers: {'Accept': 'application/json'},
    );
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for DELETE $path');
    }
  }

  String _generateJoinCode() {
    const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    final random = Random();
    return List.generate(
      6,
      (_) => alphabet[random.nextInt(alphabet.length)],
    ).join();
  }

  String _roleToApi(TeamRole role) {
    return switch (role) {
      TeamRole.mrX => 'MR_X',
      TeamRole.detective => 'DETECTIVE',
      TeamRole.spectator => 'SPECTATOR',
    };
  }
}
