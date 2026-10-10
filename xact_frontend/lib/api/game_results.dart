import 'package:latlong2/latlong.dart';

import 'models.dart';

enum GameEndReason { hostEnded, noOpponentsLeft, hostLeft, abandoned, timeUp }

enum TimelineEventType {
  gameStarted,
  mrXRevealed,
  mrXCaught,
  powerUpUsed,
  leftGameArea,
  gameEnded,
}

enum AwardType { marathon, speedDemon, hunter, ghost, transitPro, ruleBender }

GameEndReason? tryParseGameEndReason(String value) {
  return switch (value) {
    'HostEnded' => GameEndReason.hostEnded,
    'NoOpponentsLeft' => GameEndReason.noOpponentsLeft,
    'HostLeft' => GameEndReason.hostLeft,
    'Abandoned' => GameEndReason.abandoned,
    'TimeUp' => GameEndReason.timeUp,
    _ => null,
  };
}

String gameEndReasonToApi(GameEndReason reason) {
  return switch (reason) {
    GameEndReason.hostEnded => 'HostEnded',
    GameEndReason.noOpponentsLeft => 'NoOpponentsLeft',
    GameEndReason.hostLeft => 'HostLeft',
    GameEndReason.abandoned => 'Abandoned',
    GameEndReason.timeUp => 'TimeUp',
  };
}

TimelineEventType? tryParseTimelineEventType(String value) {
  return switch (value) {
    'GameStarted' => TimelineEventType.gameStarted,
    'MrXRevealed' => TimelineEventType.mrXRevealed,
    'MrXCaught' => TimelineEventType.mrXCaught,
    'PowerUpUsed' => TimelineEventType.powerUpUsed,
    'LeftGameArea' => TimelineEventType.leftGameArea,
    'GameEnded' => TimelineEventType.gameEnded,
    _ => null,
  };
}

AwardType? tryParseAwardType(String value) {
  return switch (value) {
    'Marathon' => AwardType.marathon,
    'SpeedDemon' => AwardType.speedDemon,
    'Hunter' => AwardType.hunter,
    'Ghost' => AwardType.ghost,
    'TransitPro' => AwardType.transitPro,
    'RuleBender' => AwardType.ruleBender,
    _ => null,
  };
}

int _int(Object? value) => (value as num?)?.toInt() ?? 0;

double _double(Object? value) => (value as num?)?.toDouble() ?? 0;

double? _doubleOrNull(Object? value) => (value as num?)?.toDouble();

List<Map<String, dynamic>> _list(Object? value) =>
    (value as List<dynamic>? ?? const []).cast<Map<String, dynamic>>();

LatLng? _latLngOrNull(Map<String, dynamic> json) {
  final latitude = _doubleOrNull(json['latitude']);
  final longitude = _doubleOrNull(json['longitude']);
  return latitude == null || longitude == null
      ? null
      : LatLng(latitude, longitude);
}

/// everything the end screen shows. times inside the match are seconds from
/// [startTime], which keeps the routes small
final class GameResults {
  final int sessionId;
  final String sessionName;
  final String hostUserId;
  final DateTime startTime;
  final DateTime endTime;
  final int durationSeconds;
  final GameEndReason? endReason;
  final int? winnerTeamId;
  final int revealIntervalMinutes;
  final List<ResultTeam> teams;
  final List<ResultMember> members;
  final List<MrXPeriod> mrXPeriods;
  final List<TimelineEvent> timeline;
  final List<MatchAward> awards;
  final List<LatLng> geofence;

  const GameResults({
    required this.sessionId,
    required this.sessionName,
    required this.hostUserId,
    required this.startTime,
    required this.endTime,
    required this.durationSeconds,
    required this.endReason,
    required this.winnerTeamId,
    required this.revealIntervalMinutes,
    required this.teams,
    required this.members,
    required this.mrXPeriods,
    required this.timeline,
    required this.awards,
    required this.geofence,
  });

  factory GameResults.fromJson(Map<String, dynamic> json) {
    final startTime =
        tryParseIsoDateTime(json['startTime']) ?? DateTime.now().toUtc();
    return GameResults(
      sessionId: _int(json['sessionId']),
      sessionName: (json['sessionName'] as String?) ?? 'Match',
      hostUserId: (json['hostUserId'] as String?) ?? '',
      startTime: startTime,
      endTime: tryParseIsoDateTime(json['endTime']) ?? startTime,
      durationSeconds: _int(json['durationSeconds']),
      endReason: switch (json['endReason']) {
        final String s => tryParseGameEndReason(s),
        _ => null,
      },
      winnerTeamId: (json['winnerTeamId'] as num?)?.toInt(),
      revealIntervalMinutes: _int(json['mrXRevealIntervalMinutes']),
      teams: _list(json['teams']).map(ResultTeam.fromJson).toList(),
      members: _list(json['members']).map(ResultMember.fromJson).toList(),
      mrXPeriods: _list(json['mrXPeriods']).map(MrXPeriod.fromJson).toList(),
      timeline: _list(json['timeline'])
          .map(TimelineEvent.fromJson)
          .where((e) => e.type != null)
          .toList(),
      awards: _list(
        json['awards'],
      ).map(MatchAward.fromJson).where((a) => a.type != null).toList(),
      geofence: _list(
        json['geofence'],
      ).map(_latLngOrNull).whereType<LatLng>().toList(),
    );
  }

  Duration get duration => Duration(seconds: durationSeconds);

  ResultTeam? teamById(int? teamId) {
    for (final team in teams) {
      if (team.teamId == teamId) return team;
    }
    return null;
  }

  ResultMember? memberById(int? memberId) {
    for (final member in members) {
      if (member.memberId == memberId) return member;
    }
    return null;
  }

  ResultTeam? get winnerTeam => teamById(winnerTeamId);

  List<TimelineEvent> get catches =>
      timeline.where((e) => e.type == TimelineEventType.mrXCaught).toList();

  /// players who took part, spectators only watched
  List<ResultMember> get players => members
      .where((m) => teamById(m.teamId)?.finalRole != TeamRole.spectator)
      .toList();

  /// the team that was mister x at [offsetSeconds]. at a catch the catching
  /// team already counts
  int? mrXTeamAt(int offsetSeconds) {
    int? teamId;
    for (final period in mrXPeriods) {
      if (period.fromOffsetSeconds <= offsetSeconds &&
          offsetSeconds <= period.toOffsetSeconds) {
        teamId = period.teamId;
      }
    }
    return teamId;
  }
}

final class ResultTeam {
  final int teamId;
  final String teamName;
  final TeamRole? finalRole;
  final String colorCode;

  /// the color the team hunted in. a catch swaps the colors of both teams, so
  /// [colorCode] only shows the final state. null if it was always mister x
  final String? detectiveColorCode;
  final int memberCount;
  final TeamTotals totals;

  const ResultTeam({
    required this.teamId,
    required this.teamName,
    required this.finalRole,
    required this.colorCode,
    required this.detectiveColorCode,
    required this.memberCount,
    required this.totals,
  });

  factory ResultTeam.fromJson(Map<String, dynamic> json) {
    return ResultTeam(
      teamId: _int(json['teamId']),
      teamName: (json['teamName'] as String?) ?? 'Team',
      finalRole: switch (json['finalRole']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: (json['colorCode'] as String?) ?? '#94A3B8',
      detectiveColorCode: json['detectiveColorCode'] as String?,
      memberCount: _int(json['memberCount']),
      totals: TeamTotals.fromJson(
        (json['totals'] as Map<String, dynamic>?) ?? const {},
      ),
    );
  }
}

final class TeamTotals {
  final double distanceMeters;
  final double topSpeedKmh;
  final Duration mrXTime;
  final int catchesMade;
  final int timesCaught;
  final int powerUpsUsed;
  final Duration outOfBounds;

  const TeamTotals({
    required this.distanceMeters,
    required this.topSpeedKmh,
    required this.mrXTime,
    required this.catchesMade,
    required this.timesCaught,
    required this.powerUpsUsed,
    required this.outOfBounds,
  });

  factory TeamTotals.fromJson(Map<String, dynamic> json) {
    return TeamTotals(
      distanceMeters: _double(json['distanceMeters']),
      topSpeedKmh: _double(json['topSpeedKmh']),
      mrXTime: Duration(seconds: _int(json['mrXSeconds'])),
      catchesMade: _int(json['catchesMade']),
      timesCaught: _int(json['timesCaught']),
      powerUpsUsed: _int(json['powerUpsUsed']),
      outOfBounds: Duration(seconds: _int(json['outOfBoundsSeconds'])),
    );
  }
}

final class ResultMember {
  final int memberId;
  final int teamId;
  final String displayName;
  final bool isGuest;
  final bool isHost;
  final MemberStats stats;
  final List<RoutePoint> route;

  const ResultMember({
    required this.memberId,
    required this.teamId,
    required this.displayName,
    required this.isGuest,
    required this.isHost,
    required this.stats,
    required this.route,
  });

  factory ResultMember.fromJson(Map<String, dynamic> json) {
    return ResultMember(
      memberId: _int(json['memberId']),
      teamId: _int(json['teamId']),
      displayName: (json['displayName'] as String?) ?? 'Unknown',
      isGuest: json['isGuest'] == true,
      isHost: json['isHost'] == true,
      stats: MemberStats.fromJson(
        (json['stats'] as Map<String, dynamic>?) ?? const {},
      ),
      route: _list(json['route']).map(RoutePoint.fromJson).toList(),
    );
  }
}

final class MemberStats {
  final double distanceMeters;
  final double topSpeedKmh;
  final double? avgMovingSpeedKmh;
  final double? avgPaceSecondsPerKm;
  final Duration movingTime;
  final Map<TransportMode, Duration> timeByMode;
  final Duration outOfBounds;
  final int outOfBoundsCount;
  final int powerUpsUsed;
  final Duration mrXTime;
  final int revealCount;
  final int catchesMade;

  const MemberStats({
    required this.distanceMeters,
    required this.topSpeedKmh,
    required this.avgMovingSpeedKmh,
    required this.avgPaceSecondsPerKm,
    required this.movingTime,
    required this.timeByMode,
    required this.outOfBounds,
    required this.outOfBoundsCount,
    required this.powerUpsUsed,
    required this.mrXTime,
    required this.revealCount,
    required this.catchesMade,
  });

  factory MemberStats.fromJson(Map<String, dynamic> json) {
    final timeByMode = <TransportMode, Duration>{};
    for (final entry in _list(json['timeByTransportMode'])) {
      final mode = switch (entry['mode']) {
        final String s => tryParseTransportMode(s),
        _ => null,
      };
      if (mode != null) {
        timeByMode[mode] = Duration(seconds: _int(entry['seconds']));
      }
    }

    return MemberStats(
      distanceMeters: _double(json['distanceMeters']),
      topSpeedKmh: _double(json['topSpeedKmh']),
      avgMovingSpeedKmh: _doubleOrNull(json['avgMovingSpeedKmh']),
      avgPaceSecondsPerKm: _doubleOrNull(json['avgPaceSecondsPerKm']),
      movingTime: Duration(seconds: _int(json['movingSeconds'])),
      timeByMode: timeByMode,
      outOfBounds: Duration(seconds: _int(json['outOfBoundsSeconds'])),
      outOfBoundsCount: _int(json['outOfBoundsCount']),
      powerUpsUsed: _int(json['powerUpsUsed']),
      mrXTime: Duration(seconds: _int(json['mrXSeconds'])),
      revealCount: _int(json['revealCount']),
      catchesMade: _int(json['catchesMade']),
    );
  }

  /// the transport split is only worth showing once someone left their feet
  bool get usedTransit =>
      timeByMode.entries.any((e) => e.key != TransportMode.foot && e.value > Duration.zero);
}

final class RoutePoint {
  final int offsetSeconds;
  final LatLng position;
  final TransportMode? mode;
  final bool isRevealed;

  const RoutePoint({
    required this.offsetSeconds,
    required this.position,
    required this.mode,
    required this.isRevealed,
  });

  factory RoutePoint.fromJson(Map<String, dynamic> json) {
    return RoutePoint(
      offsetSeconds: _int(json['offsetSeconds']),
      position: LatLng(_double(json['latitude']), _double(json['longitude'])),
      mode: switch (json['transportMode']) {
        final String s => tryParseTransportMode(s),
        _ => null,
      },
      isRevealed: json['isRevealed'] == true,
    );
  }
}

final class MrXPeriod {
  final int teamId;
  final int fromOffsetSeconds;
  final int toOffsetSeconds;

  const MrXPeriod({
    required this.teamId,
    required this.fromOffsetSeconds,
    required this.toOffsetSeconds,
  });

  factory MrXPeriod.fromJson(Map<String, dynamic> json) {
    return MrXPeriod(
      teamId: _int(json['teamId']),
      fromOffsetSeconds: _int(json['fromOffsetSeconds']),
      toOffsetSeconds: _int(json['toOffsetSeconds']),
    );
  }
}

final class TimelineEvent {
  final int offsetSeconds;
  final DateTime? occurredAt;
  final TimelineEventType? type;
  final int? teamId;
  final int? memberId;
  final int? otherTeamId;
  final LatLng? position;
  final PowerUpType? powerUpType;
  final Duration? duration;

  const TimelineEvent({
    required this.offsetSeconds,
    required this.occurredAt,
    required this.type,
    required this.teamId,
    required this.memberId,
    required this.otherTeamId,
    required this.position,
    required this.powerUpType,
    required this.duration,
  });

  factory TimelineEvent.fromJson(Map<String, dynamic> json) {
    final durationSeconds = (json['durationSeconds'] as num?)?.toInt();
    return TimelineEvent(
      offsetSeconds: _int(json['offsetSeconds']),
      occurredAt: tryParseIsoDateTime(json['occurredAt']),
      type: switch (json['type']) {
        final String s => tryParseTimelineEventType(s),
        _ => null,
      },
      teamId: (json['teamId'] as num?)?.toInt(),
      memberId: (json['memberId'] as num?)?.toInt(),
      otherTeamId: (json['otherTeamId'] as num?)?.toInt(),
      position: _latLngOrNull(json),
      powerUpType: switch (json['powerUpType']) {
        final String s => tryParsePowerUpType(s),
        _ => null,
      },
      duration: durationSeconds == null
          ? null
          : Duration(seconds: durationSeconds),
    );
  }
}

final class MatchAward {
  final AwardType? type;
  final List<int> memberIds;

  /// the winning metric: meters, km/h, catches or seconds depending on [type]
  final double value;

  const MatchAward({
    required this.type,
    required this.memberIds,
    required this.value,
  });

  factory MatchAward.fromJson(Map<String, dynamic> json) {
    return MatchAward(
      type: switch (json['type']) {
        final String s => tryParseAwardType(s),
        _ => null,
      },
      memberIds: (json['memberIds'] as List<dynamic>? ?? const [])
          .map((id) => (id as num).toInt())
          .toList(),
      value: _double(json['value']),
    );
  }
}
