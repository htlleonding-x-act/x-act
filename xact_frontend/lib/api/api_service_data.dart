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
}