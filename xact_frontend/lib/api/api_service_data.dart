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
            teamId: team.teamId,
            teamName: team.teamName,
            role: team.role,
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
      role: team.role,
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
    if (details.revealIntervalSeconds <= 0) {
      return const MapHeaderData(nextPingText: 'Next ping: -');
    }

    final remainingSeconds = _resolveRemainingRevealSeconds(details);
    final display = _formatDuration(remainingSeconds);
    return MapHeaderData(
      nextPingText: 'Next ping: $display',
      remainingSeconds: remainingSeconds,
      intervalSeconds: details.revealIntervalSeconds,
    );
  }

  Future<List<MapLegendTeamData>> loadMapLegendTeams(int sessionId) async {
    final snapshot = await loadLobbySnapshot(sessionId);

    final currentTeamId = _session.currentTeamId;
    final currentTeamRole = currentTeamId == null
        ? null
        : snapshot.teams
              .where((team) => team.teamId == currentTeamId)
              .map((team) => team.role)
              .firstOrNull;

    final visibleTeams = <MapLegendTeamData>[];

    for (final team in snapshot.teams) {
      if (team.role == TeamRole.spectator) {
        continue;
      }

      // Keep legend visibility aligned with marker visibility (e.g. Mr. X should not see detective teams in legend).
      if (currentTeamRole == TeamRole.mrX && team.role != TeamRole.mrX) {
        continue;
      }

      visibleTeams.add(
        MapLegendTeamData(
          teamId: team.teamId,
          label: formatTeamNameWithRole(team.teamName, team.role),
          color: tryParseHexColor(team.colorCode) ?? Colors.blueGrey,
        ),
      );
    }

    return visibleTeams;
  }

  // Visibility rules for player markers on the map:
  //   - Spectators / unassigned: no map presence.
  //   - Detectives: always visible to every viewer.
  //   - Mr. X: only visible when the latest log is a revealed ping. The
  //     marker is placed at the revealed log's coordinates so the position
  //     does not leak between reveals. The backend snapshot service already
  //     filters Mr. X's latestLocations down to revealed entries, so an
  //     absent / non-revealed entry here means "hidden".
  //   - The current player's own marker is rendered from local GPS by the
  //     map widget, not from this list, so these rules apply to everyone
  //     else.
  //   - When the current viewer is Mr. X, every other player is hidden — Mr.
  //     X must not learn detective positions from the map. The "You" marker
  //     still comes from local GPS in the map widget.
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

    if (currentTeamRole == TeamRole.mrX) {
      return out;
    }

    final latestLocationByMemberId = <int, SnapshotLatestLocation>{};
    for (final location in snapshot.latestLocations) {
      latestLocationByMemberId[location.memberId] = location;
    }

    for (final team in snapshot.teams) {
      if (team.role == TeamRole.spectator) {
        continue;
      }

      final color = tryParseHexColor(team.colorCode) ?? Colors.blueGrey;
      final members = snapshot.membersByTeamId[team.teamId] ?? const [];
      final isMrXTeam = team.role == TeamRole.mrX;

      for (final member in members) {
        double? lat;
        double? lon;

        if (isMrXTeam) {
          final revealed = latestLocationByMemberId[member.memberId];
          if (revealed == null || !revealed.isRevealedPosition) {
            continue;
          }
          lat = revealed.latitude;
          lon = revealed.longitude;
        } else {
          lat = member.currentLatitude;
          lon = member.currentLongitude;
        }

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

int _resolveRemainingRevealSeconds(GameSessionDetails details) {
  final nextRevealAt = details.nextRevealAt;
  final serverNow = details.serverNow;

  if (nextRevealAt != null) {
    final computedSeconds = nextRevealAt.difference(serverNow).inSeconds;
    if (computedSeconds > 0) {
      return computedSeconds;
    }
  }

  return details.revealSecondsRemaining;
}
