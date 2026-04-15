part of 'api_service.dart';

extension ApiServiceDataMethods on ApiService {
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
    if (details.revealIntervalSeconds <= 0 || details.revealSecondsRemaining <= 0) {
      return const MapHeaderData(nextPingText: 'Next ping: -');
    }

    final display = _formatDuration(details.revealSecondsRemaining);
    return MapHeaderData(
      nextPingText: 'Next ping: $display',
      remainingSeconds: details.revealSecondsRemaining,
      intervalSeconds: details.revealIntervalSeconds,
    );
  }

  Future<List<PlayerPositionData>> loadPlayerPositions(int sessionId) async {
    final snapshot = await loadLobbySnapshot(sessionId);
    final out = <PlayerPositionData>[];

    final currentTeamId = _session.currentTeamId;
    final currentTeamRole = currentTeamId == null
        ? null
        : snapshot.teams
              .where((team) => team.teamId == currentTeamId)
              .map((team) => team.role)
              .firstOrNull;

    // Create a map of memberId -> latest location for reveal check
    final latestLocationByMemberId = <int, SnapshotLatestLocation>{};
    for (final location in snapshot.latestLocations) {
      latestLocationByMemberId[location.memberId] = location;
    }

    for (final team in snapshot.teams) {
      final color = tryParseHexColor(team.colorCode) ?? Colors.blueGrey;
      final members = snapshot.membersByTeamId[team.teamId] ?? const [];

      // TODO: Final visibility rules are not fully defined yet.
      // Temporary behavior: Mister X should not see detective pings.
      if (currentTeamRole == TeamRole.mrX && team.role != TeamRole.mrX) {
        continue;
      }

      for (final member in members) {
        final lat = member.currentLatitude;
        final lon = member.currentLongitude;
        if (lat == null || lon == null) {
          continue;
        }

        // For Mr. X team, only show position if it's a revealed position
        if (team.role == TeamRole.mrX) {
          final latestLocation = latestLocationByMemberId[member.memberId];
          if (latestLocation == null || !latestLocation.isRevealedPosition) {
            continue;
          }
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
}

String _formatDuration(int totalSeconds) {
  final minutes = totalSeconds ~/ 60;
  final seconds = totalSeconds % 60;

  if (minutes <= 0) {
    return '${seconds}s';
  }

  return seconds == 0
      ? '${minutes}m'
      : '${minutes}m ${seconds.toString().padLeft(2, '0')}s';
}
