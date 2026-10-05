part of 'api_service.dart';

final class ApiException implements Exception {
  ApiException(this.statusCode, this.code);

  final int statusCode;
  /// the `code` of a backend domain error, e.g. `name_taken_in_session`
  final String? code;

  @override
  String toString() => 'HTTP $statusCode${code == null ? '' : ' ($code)'}';
}

/// turns a failed request into a sentence a player can act on, instead of
/// showing the raw exception
String describeApiError(Object error) => switch (error) {
  ApiException(code: 'session_not_joinable') =>
    'This game has already started or is over.',
  ApiException(code: 'session_not_active') => 'This game is not running.',
  ApiException(code: 'name_taken_in_session') =>
    'Someone in this game already uses that name. Pick another one.',
  ApiException(code: 'username_taken') =>
    'That name belongs to a registered player. Pick another one.',
  ApiException(code: 'team_has_members') =>
    'Move the players out of the team first.',
  ApiException(statusCode: 404) => 'This game no longer exists.',
  ApiException(statusCode: 400) => 'Please check your input and try again.',
  ApiException() => 'The server could not handle that. Please try again.',
  http.ClientException() =>
    'Could not reach the server. Check your connection.',
  _ => 'Something went wrong. Please try again.',
};

final class TeamCardData {
  final int teamId;
  final String teamName;
  final TeamRole? role;
  final Color color;
  final bool isMisterX;
  final List<String> members;

  const TeamCardData({
    required this.teamId,
    required this.teamName,
    required this.role,
    required this.color,
    required this.isMisterX,
    required this.members,
  });
}

final class TeamChatHeaderData {
  final String teamName;
  final TeamRole? role;
  final int memberCount;
  final Color teamColor;

  const TeamChatHeaderData({
    required this.teamName,
    required this.role,
    required this.memberCount,
    required this.teamColor,
  });
}

final class MapHeaderData {
  final String nextPingText;
  final int remainingSeconds;
  final int intervalSeconds;

  double get progress =>
      intervalSeconds > 0 ? 1.0 - (remainingSeconds / intervalSeconds) : 0.0;

  const MapHeaderData({
    required this.nextPingText,
    this.remainingSeconds = 0,
    this.intervalSeconds = 0,
  });
}

final class MapLegendTeamData {
  final int teamId;
  final String label;
  final Color color;

  const MapLegendTeamData({
    required this.teamId,
    required this.label,
    required this.color,
  });
}

final class PlayerPositionData {
  final int memberId;
  final int teamId;
  final String displayName;
  final TeamRole? teamRole;
  final Color color;
  final LatLng position;
  /// when mister x was revealed at [position], null for detectives
  final DateTime? revealedAt;

  const PlayerPositionData({
    required this.memberId,
    required this.teamId,
    required this.displayName,
    required this.teamRole,
    required this.color,
    required this.position,
    this.revealedAt,
  });
}

final class LobbySnapshot {
  final List<TeamDetails> teams;
  final Map<int, List<TeamMemberDetails>> membersByTeamId;
  final Map<String, UserInfo> usersById;
  final List<SnapshotLatestLocation> latestLocations;

  const LobbySnapshot({
    required this.teams,
    required this.membersByTeamId,
    required this.usersById,
    this.latestLocations = const [],
  });
}