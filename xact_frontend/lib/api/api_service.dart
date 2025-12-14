import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

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

  const MapHeaderData({required this.nextPingText});
}

final class ApiService {
  ApiService._({required String baseUrl})
    : _baseUri = Uri.parse(baseUrl),
      _http = http.Client();

  static final ApiService instance = ApiService._(baseUrl: ApiConfig.baseUrl);

  final Uri _baseUri;
  final http.Client _http;

  Future<List<TeamCardData>> loadTeamCards() async {
    final sessionId = await _getActiveSessionId();

    final teams = await _listTeams();
    final teamMembers = await _listTeamMembers();
    final users = await _listUsers();

    final usersById = {for (final u in users) u.userId: u};
    final membersByTeamId = <String, List<TeamMemberInfo>>{};
    for (final member in teamMembers) {
      membersByTeamId.putIfAbsent(member.teamId, () => []).add(member);
    }

    final teamsForSession = await _filterTeamsForSession(teams, sessionId);

    return teamsForSession
        .map((team) {
          final color = tryParseHexColor(team.colorCode) ?? Colors.white;
          final memberNames = (membersByTeamId[team.teamId] ?? const [])
              .map((m) => usersById[m.userId]?.username ?? m.userId)
              .toList(growable: false);

          return TeamCardData(
            teamName: team.teamName,
            color: color,
            isMisterX: team.role == TeamRole.mrX,
            members: memberNames,
          );
        })
        .toList(growable: false);
  }

  Future<TeamChatHeaderData> loadTeamChatHeader() async {
    final sessionId = await _getActiveSessionId();

    final teams = await _listTeams();
    final teamMembers = await _listTeamMembers();

    final teamsForSession = await _filterTeamsForSession(teams, sessionId);
    if (teamsForSession.isEmpty) {
      throw StateError('No teams found.');
    }

    final team = teamsForSession.firstWhere(
      (t) => t.role == TeamRole.detective,
      orElse: () => teamsForSession.first,
    );

    final memberCount = teamMembers
        .where((m) => m.teamId == team.teamId)
        .length;
    final color = tryParseHexColor(team.colorCode) ?? Colors.blue;

    return TeamChatHeaderData(
      teamName: team.teamName,
      memberCount: memberCount,
      teamColor: color,
    );
  }

  Future<MapHeaderData> loadMapHeader() async {
    final sessionId = await _getActiveSessionId();
    if (sessionId == null) {
      return const MapHeaderData(nextPingText: 'Next ping: —');
    }

    final details = await _getGameSession(sessionId);
    final start = details.startTime;
    final interval = details.mrXRevealInterval;

    if (start == null || interval <= 0) {
      return const MapHeaderData(nextPingText: 'Next ping: —');
    }

    final now = DateTime.now().toUtc();
    final elapsedMinutes = now.difference(start.toUtc()).inMinutes;
    final minutesIntoCycle = elapsedMinutes % interval;
    final remaining = (interval - minutesIntoCycle) % interval;

    final display = remaining == 0 ? interval : remaining;
    return MapHeaderData(nextPingText: 'Next ping: ${display}m');
  }

  Future<String?> _getActiveSessionId() async {
    final sessions = await _listGameSessions();
    final active = sessions
        .where((s) => s.status == SessionStatus.active)
        .toList();
    if (active.isNotEmpty) {
      return active.first.sessionId;
    }
    return sessions.isEmpty ? null : sessions.first.sessionId;
  }

  Future<List<TeamInfo>> _filterTeamsForSession(
    List<TeamInfo> teams,
    String? sessionId,
  ) async {
    if (sessionId == null) {
      return teams;
    }

    final details = await Future.wait(teams.map((t) => _getTeam(t.teamId)));
    final byId = {for (final d in details) d.teamId: d};

    return teams
        .where((t) => byId[t.teamId]?.sessionId == sessionId)
        .toList(growable: false);
  }

  Future<List<UserInfo>> _listUsers() async {
    final json = await _getJsonObject('/api/users');
    return ApiListResponse.fromJson(json, UserInfo.fromJson).items;
  }

  Future<List<GameSessionInfo>> _listGameSessions() async {
    final json = await _getJsonObject('/api/gamesessions');
    return ApiListResponse.fromJson(json, GameSessionInfo.fromJson).items;
  }

  Future<GameSessionDetails> _getGameSession(String sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId');
    return GameSessionDetails.fromJson(json);
  }

  Future<List<TeamInfo>> _listTeams() async {
    final json = await _getJsonObject('/api/teams');
    return ApiListResponse.fromJson(json, TeamInfo.fromJson).items;
  }

  Future<TeamDetails> _getTeam(String teamId) async {
    final json = await _getJsonObject('/api/teams/$teamId');
    return TeamDetails.fromJson(json);
  }

  Future<List<TeamMemberInfo>> _listTeamMembers() async {
    final json = await _getJsonObject('/api/teammembers');
    return ApiListResponse.fromJson(json, TeamMemberInfo.fromJson).items;
  }

  Future<Map<String, dynamic>> _getJsonObject(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.get(
      uri,
      headers: {'Accept': 'application/json'},
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for $path');
    }

    final decoded = jsonDecode(response.body);
    if (decoded is Map<String, dynamic>) {
      return decoded;
    }

    throw const FormatException('Expected a JSON object.');
  }
}
