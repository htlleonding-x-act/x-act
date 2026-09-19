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

  Future<List<MapLegendTeamData>> loadMapLegendTeams(
    int sessionId, {
    LobbySnapshot? from,
  }) async {
    final snapshot = from ?? await loadLobbySnapshot(sessionId);

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

      // same rule as the markers below: mr.x doesn't see detective teams in the legend either
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

  // who shows up on the map:
  //   - spectators and unassigned players: nobody sees them
  //   - detectives: always visible to every viewer
  //   - mr.x: only when the latest log is a revealed ping, placed at that log's
  //     coordinates so the position doesn't leak between reveals. the backend
  //     snapshot already filters mr.x down to revealed entries, so a missing or
  //     unrevealed entry means hidden
  //   - when the viewer is mr.x, every other player is hidden so mr.x can't
  //     learn detective positions from the map
  // the viewer's own marker comes from local gps in the map widget, not from
  // this list, so these rules only cover everyone else
  Future<List<PlayerPositionData>> loadPlayerPositions(
    int sessionId, {
    LobbySnapshot? from,
  }) async {
    final snapshot = from ?? await loadLobbySnapshot(sessionId);
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
