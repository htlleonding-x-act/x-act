part of 'api_service.dart';

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