import 'dart:ui';

Color? tryParseHexColor(String value) {
  final normalized = value.trim();
  final hex = normalized.startsWith('#') ? normalized.substring(1) : normalized;
  if (hex.length != 6 && hex.length != 8) {
    return null;
  }

  final parsed = int.tryParse(hex, radix: 16);
  if (parsed == null) {
    return null;
  }

  if (hex.length == 6) {
    return Color(0xFF000000 | parsed);
  }

  return Color(parsed);
}

DateTime? tryParseIsoDateTime(Object? value) {
  if (value is! String) {
    return null;
  }
  return DateTime.tryParse(value);
}

int _readInt(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is num) {
      return value.toInt();
    }
  }
  throw FormatException('Expected one of ${keys.join(', ')} to be numeric.');
}

int? _readNullableInt(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    final value = json[key];
    if (value is num) {
      return value.toInt();
    }
  }
  return null;
}

final class ApiListResponse<T> {
  final List<T> items;

  const ApiListResponse({required this.items});

  static ApiListResponse<T> fromJson<T>(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromItem,
  ) {
    final rawItems = json['items'];
    if (rawItems is! List) {
      throw const FormatException('Expected "items" to be a list.');
    }

    return ApiListResponse<T>(
      items: rawItems
          .whereType<Map>()
          .map((e) => e.cast<String, dynamic>())
          .map(fromItem)
          .toList(growable: false),
    );
  }
}

enum SessionStatus { waiting, active, finished }

enum TeamRole { mrX, detective, spectator }

enum AccountType { free, pro, eventPass }

enum TransportMode { foot, bus, tram, train }

enum PowerUpType { blackTicket, doubleMove }

SessionStatus? tryParseSessionStatus(String value) {
  return switch (value) {
    'Waiting' => SessionStatus.waiting,
    'Active' => SessionStatus.active,
    'Finished' => SessionStatus.finished,
    'WAITING' => SessionStatus.waiting,
    'ACTIVE' => SessionStatus.active,
    'FINISHED' => SessionStatus.finished,
    _ => null,
  };
}

TeamRole? tryParseTeamRole(String value) {
  return switch (value) {
    'MrX' => TeamRole.mrX,
    'Detective' => TeamRole.detective,
    'Spectator' => TeamRole.spectator,
    'MR_X' => TeamRole.mrX,
    'DETECTIVE' => TeamRole.detective,
    'SPECTATOR' => TeamRole.spectator,
    _ => null,
  };
}

String teamRoleDisplayLabel(TeamRole? role) {
  return switch (role) {
    TeamRole.mrX => 'Mister X',
    TeamRole.detective => 'Detective',
    TeamRole.spectator => 'Spectator',
    null => 'Unknown',
  };
}

String formatTeamNameWithRole(String teamName, TeamRole? role) {
  if (role == null || role == TeamRole.spectator) {
    return teamName;
  }

  return '$teamName (${teamRoleDisplayLabel(role)})';
}

AccountType? tryParseAccountType(String value) {
  return switch (value) {
    'FREE' => AccountType.free,
    'PRO' => AccountType.pro,
    'EVENT_PASS' => AccountType.eventPass,
    _ => null,
  };
}

TransportMode? tryParseTransportMode(String value) {
  return switch (value) {
    'Foot' => TransportMode.foot,
    'Bus' => TransportMode.bus,
    'Tram' => TransportMode.tram,
    'Train' => TransportMode.train,
    'FOOT' => TransportMode.foot,
    'BUS' => TransportMode.bus,
    'TRAM' => TransportMode.tram,
    'TRAIN' => TransportMode.train,
    _ => null,
  };
}

PowerUpType? tryParsePowerUpType(String value) {
  return switch (value) {
    'BlackTicket' => PowerUpType.blackTicket,
    'DoubleMove' => PowerUpType.doubleMove,
    'BLACK_TICKET' => PowerUpType.blackTicket,
    'DOUBLE_MOVE' => PowerUpType.doubleMove,
    _ => null,
  };
}

final class UserInfo {
  final String userId;
  final String username;
  final String email;
  final AccountType? accountType;

  const UserInfo({
    required this.userId,
    required this.username,
    required this.email,
    required this.accountType,
  });

  factory UserInfo.fromJson(Map<String, dynamic> json) {
    return UserInfo(
      userId: json['id'] as String,
      username: json['username'] as String,
      email: json['email'] as String,
      accountType: switch (json['accountType']) {
        final String s => tryParseAccountType(s),
        _ => null,
      },
    );
  }
}

final class UserDetails {
  final String userId;
  final String username;
  final String email;
  final AccountType? accountType;
  final DateTime? subscriptionEndDate;
  final int totalWins;
  final int totalGamesPlayed;

  const UserDetails({
    required this.userId,
    required this.username,
    required this.email,
    required this.accountType,
    required this.subscriptionEndDate,
    required this.totalWins,
    required this.totalGamesPlayed,
  });

  factory UserDetails.fromJson(Map<String, dynamic> json) {
    return UserDetails(
      userId: json['id'] as String,
      username: json['username'] as String,
      email: json['email'] as String,
      accountType: switch (json['accountType']) {
        final String s => tryParseAccountType(s),
        _ => null,
      },
      subscriptionEndDate: tryParseIsoDateTime(json['subscriptionEndDate']),
      totalWins: (json['totalWins'] as num).toInt(),
      totalGamesPlayed: (json['totalGamesPlayed'] as num).toInt(),
    );
  }
}

final class GameSessionInfo {
  final int sessionId;
  final String sessionName;
  final String joinCode;
  final SessionStatus? status;
  final DateTime? startTime;
  final DateTime? endTime;

  const GameSessionInfo({
    required this.sessionId,
    required this.sessionName,
    required this.joinCode,
    required this.status,
    required this.startTime,
    required this.endTime,
  });

  factory GameSessionInfo.fromJson(Map<String, dynamic> json) {
    return GameSessionInfo(
      sessionId: _readInt(json, ['id', 'sessionId']),
      sessionName: (json['sessionName'] as String?) ?? 'Session',
      joinCode: json['joinCode'] as String,
      status: switch (json['status']) {
        final String s => tryParseSessionStatus(s),
        _ => null,
      },
      startTime: tryParseIsoDateTime(json['startTime']),
      endTime: tryParseIsoDateTime(json['endTime']),
    );
  }
}

final class GameSessionDetails {
  final int sessionId;
  final String hostUserId;
  final String sessionName;
  final String joinCode;
  final SessionStatus? status;
  final DateTime? startTime;
  final DateTime? endTime;
  final int plannedDurationMinutes;
  final int mrXRevealInterval;
  final DateTime serverNow;
  final DateTime? nextRevealAt;
  final int revealSecondsRemaining;
  final int revealIntervalSeconds;

  const GameSessionDetails({
    required this.sessionId,
    required this.hostUserId,
    required this.sessionName,
    required this.joinCode,
    required this.status,
    required this.startTime,
    required this.endTime,
    required this.plannedDurationMinutes,
    required this.mrXRevealInterval,
    required this.serverNow,
    required this.nextRevealAt,
    required this.revealSecondsRemaining,
    required this.revealIntervalSeconds,
  });

  factory GameSessionDetails.fromJson(Map<String, dynamic> json) {
    return GameSessionDetails(
      sessionId: _readInt(json, ['id', 'sessionId']),
      hostUserId: json['hostUserId'] as String,
      sessionName: (json['sessionName'] as String?) ?? 'Session',
      joinCode: json['joinCode'] as String,
      status: switch (json['status']) {
        final String s => tryParseSessionStatus(s),
        _ => null,
      },
      startTime: tryParseIsoDateTime(json['startTime']),
      endTime: tryParseIsoDateTime(json['endTime']),
      plannedDurationMinutes: (json['plannedDurationMinutes'] as num).toInt(),
      mrXRevealInterval: (json['mrXRevealInterval'] as num).toInt(),
      serverNow:
          tryParseIsoDateTime(json['serverNow']) ?? DateTime.now().toUtc(),
      nextRevealAt: tryParseIsoDateTime(json['nextRevealAt']),
      revealSecondsRemaining: _readInt(json, ['revealSecondsRemaining']),
      revealIntervalSeconds: _readInt(json, ['revealIntervalSeconds']),
    );
  }
}

final class TeamInfo {
  final int teamId;
  final int sessionId;
  final String teamName;
  final TeamRole? role;
  final String colorCode;
  final int maxPlayerCount;

  const TeamInfo({
    required this.teamId,
    required this.sessionId,
    required this.teamName,
    required this.role,
    required this.colorCode,
    required this.maxPlayerCount,
  });

  factory TeamInfo.fromJson(Map<String, dynamic> json) {
    return TeamInfo(
      teamId: _readInt(json, ['id', 'teamId']),
      sessionId: _readInt(json, ['sessionId']),
      teamName: json['teamName'] as String,
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: json['colorCode'] as String,
      maxPlayerCount: _readInt(json, ['maxPlayerCount']),
    );
  }
}

final class TeamDetails {
  final int teamId;
  final int sessionId;
  final String teamName;
  final TeamRole? role;
  final String colorCode;
  final bool isCaught;
  final int maxPlayerCount;

  const TeamDetails({
    required this.teamId,
    required this.sessionId,
    required this.teamName,
    required this.role,
    required this.colorCode,
    required this.isCaught,
    required this.maxPlayerCount,
  });

  factory TeamDetails.fromJson(Map<String, dynamic> json) {
    return TeamDetails(
      teamId: _readInt(json, ['id', 'teamId']),
      sessionId: _readInt(json, ['sessionId']),
      teamName: json['teamName'] as String,
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: json['colorCode'] as String,
      isCaught: json['isCaught'] as bool,
      maxPlayerCount: _readInt(json, ['maxPlayerCount']),
    );
  }
}

final class TeamMemberInfo {
  final int memberId;
  final int teamId;
  final int sessionId;
  final String? userId;
  final String? guestName;
  final bool isTeamLeader;

  const TeamMemberInfo({
    required this.memberId,
    required this.teamId,
    required this.sessionId,
    required this.userId,
    required this.guestName,
    required this.isTeamLeader,
  });

  factory TeamMemberInfo.fromJson(Map<String, dynamic> json) {
    return TeamMemberInfo(
      memberId: _readInt(json, ['id', 'memberId']),
      teamId: _readInt(json, ['teamId']),
      sessionId: _readInt(json, ['sessionId']),
      userId: json['userId'] as String?,
      guestName: json['guestName'] as String?,
      isTeamLeader: json['isTeamLeader'] as bool,
    );
  }
}

final class TeamMemberDetails {
  final int memberId;
  final int teamId;
  final int sessionId;
  final String? userId;
  final String? guestName;
  final bool isTeamLeader;
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime? lastUpdated;

  const TeamMemberDetails({
    required this.memberId,
    required this.teamId,
    required this.sessionId,
    required this.userId,
    required this.guestName,
    required this.isTeamLeader,
    required this.currentLatitude,
    required this.currentLongitude,
    required this.lastUpdated,
  });

  factory TeamMemberDetails.fromJson(Map<String, dynamic> json) {
    return TeamMemberDetails(
      memberId: _readInt(json, ['id', 'memberId']),
      teamId: _readInt(json, ['teamId']),
      sessionId: _readInt(json, ['sessionId']),
      userId: json['userId'] as String?,
      guestName: json['guestName'] as String?,
      isTeamLeader: json['isTeamLeader'] as bool,
      currentLatitude: (json['currentLatitude'] as num?)?.toDouble(),
      currentLongitude: (json['currentLongitude'] as num?)?.toDouble(),
      lastUpdated: tryParseIsoDateTime(json['lastUpdated']),
    );
  }
}

final class GeofencePointInfo {
  final int pointId;
  final int sessionId;
  final double latitude;
  final double longitude;

  const GeofencePointInfo({
    required this.pointId,
    required this.sessionId,
    required this.latitude,
    required this.longitude,
  });

  factory GeofencePointInfo.fromJson(Map<String, dynamic> json) {
    return GeofencePointInfo(
      pointId: _readInt(json, ['id', 'pointId']),
      sessionId: (json['sessionId'] as num).toInt(),
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
    );
  }
}

final class GeofencePointDetails {
  final int pointId;
  final int sessionId;
  final double latitude;
  final double longitude;
  final int sequenceOrder;

  const GeofencePointDetails({
    required this.pointId,
    required this.sessionId,
    required this.latitude,
    required this.longitude,
    required this.sequenceOrder,
  });

  factory GeofencePointDetails.fromJson(Map<String, dynamic> json) {
    return GeofencePointDetails(
      pointId: _readInt(json, ['id', 'pointId']),
      sessionId: (json['sessionId'] as num).toInt(),
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      sequenceOrder: (json['sequenceOrder'] as num).toInt(),
    );
  }
}

final class LocationLogInfo {
  final int logId;
  final int memberId;
  final DateTime timestamp;
  final double latitude;
  final double longitude;

  const LocationLogInfo({
    required this.logId,
    required this.memberId,
    required this.timestamp,
    required this.latitude,
    required this.longitude,
  });

  factory LocationLogInfo.fromJson(Map<String, dynamic> json) {
    return LocationLogInfo(
      logId: _readInt(json, ['id', 'logId']),
      memberId: (json['memberId'] as num).toInt(),
      timestamp: DateTime.parse(json['timestamp'] as String),
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
    );
  }
}

final class LocationLogDetails {
  final int logId;
  final int memberId;
  final DateTime timestamp;
  final double latitude;
  final double longitude;
  final double accuracyMeters;
  final TransportMode? transportMode;
  final bool isRevealedPosition;

  const LocationLogDetails({
    required this.logId,
    required this.memberId,
    required this.timestamp,
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.transportMode,
    required this.isRevealedPosition,
  });

  factory LocationLogDetails.fromJson(Map<String, dynamic> json) {
    return LocationLogDetails(
      logId: _readInt(json, ['id', 'logId']),
      memberId: (json['memberId'] as num).toInt(),
      timestamp: DateTime.parse(json['timestamp'] as String),
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      accuracyMeters: (json['accuracyMeters'] as num).toDouble(),
      transportMode: switch (json['transportMode']) {
        final String s => tryParseTransportMode(s),
        _ => null,
      },
      isRevealedPosition: json['isRevealedPosition'] as bool,
    );
  }
}

final class PowerUpUsageInfo {
  final int usageId;
  final int memberId;
  final PowerUpType? powerUpType;
  final DateTime usedAt;

  const PowerUpUsageInfo({
    required this.usageId,
    required this.memberId,
    required this.powerUpType,
    required this.usedAt,
  });

  factory PowerUpUsageInfo.fromJson(Map<String, dynamic> json) {
    return PowerUpUsageInfo(
      usageId: _readInt(json, ['id', 'usageId']),
      memberId: (json['memberId'] as num).toInt(),
      powerUpType: switch (json['powerUpType']) {
        final String s => tryParsePowerUpType(s),
        _ => null,
      },
      usedAt: DateTime.parse(json['usedAt'] as String),
    );
  }
}

final class PowerUpUsageDetails {
  final int usageId;
  final int memberId;
  final PowerUpType? powerUpType;
  final DateTime usedAt;

  const PowerUpUsageDetails({
    required this.usageId,
    required this.memberId,
    required this.powerUpType,
    required this.usedAt,
  });

  factory PowerUpUsageDetails.fromJson(Map<String, dynamic> json) {
    return PowerUpUsageDetails(
      usageId: _readInt(json, ['id', 'usageId']),
      memberId: (json['memberId'] as num).toInt(),
      powerUpType: switch (json['powerUpType']) {
        final String s => tryParsePowerUpType(s),
        _ => null,
      },
      usedAt: DateTime.parse(json['usedAt'] as String),
    );
  }
}

final class RealtimeMethods {
  static const String event = 'realtime_event';
  static const String snapshot = 'realtime_snapshot';
}

final class RealtimeEvents {
  static const String teamAdded = 'team_added';
  static const String teamUpdated = 'team_updated';
  static const String teamDeleted = 'team_deleted';
  static const String teamMemberJoined = 'team_member_joined';
  static const String teamMemberUpdated = 'team_member_updated';
  static const String teamMemberLeft = 'team_member_left';
  static const String gameSessionStarted = 'game_session_started';
  static const String locationLogRecorded = 'location_log_recorded';
  static const String mrXCaught = 'mr_x_caught';
  static const String chatMessagePosted = 'chat_message_posted';
}

final class RealtimeEventEnvelope {
  final String type;
  final Map<String, dynamic> payload;

  const RealtimeEventEnvelope({required this.type, required this.payload});

  factory RealtimeEventEnvelope.fromJson(Map<String, dynamic> json) {
    final payload = json['payload'];
    return RealtimeEventEnvelope(
      type: json['type'] as String,
      payload: payload is Map
          ? payload.cast<String, dynamic>()
          : <String, dynamic>{},
    );
  }
}

/// A single chat message. Parses both the REST DTO (`id`) and the realtime
/// `chat_message_posted` payload (`messageId`). A `null` [teamId] denotes the
/// global "All" channel; a non-null value is that team's private channel.
final class ChatMessage {
  final int id;
  final int sessionId;
  final int? teamId;
  final int? senderMemberId;
  final int? senderTeamId;
  final String senderName;
  final String content;
  final DateTime sentAt;

  const ChatMessage({
    required this.id,
    required this.sessionId,
    required this.teamId,
    required this.senderMemberId,
    required this.senderTeamId,
    required this.senderName,
    required this.content,
    required this.sentAt,
  });

  bool get isGlobal => teamId == null;

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    return ChatMessage(
      id: _readInt(json, ['id', 'messageId']),
      sessionId: _readInt(json, ['sessionId']),
      teamId: _readNullableInt(json, ['teamId']),
      senderMemberId: _readNullableInt(json, ['senderMemberId']),
      senderTeamId: _readNullableInt(json, ['senderTeamId']),
      senderName: (json['senderName'] as String?) ?? 'Unknown',
      content: (json['content'] as String?) ?? '',
      sentAt: tryParseIsoDateTime(json['sentAt']) ?? DateTime.now().toUtc(),
    );
  }
}

final class GameSessionSnapshot {
  final int sessionId;
  final String sessionName;
  final SessionStatus? status;
  final DateTime? startTime;
  final DateTime? endTime;
  final int plannedDurationMinutes;
  final int mrXRevealInterval;
  final List<SnapshotTeam> teams;
  final List<SnapshotTeamMember> members;
  final List<SnapshotLatestLocation> latestLocations;

  const GameSessionSnapshot({
    required this.sessionId,
    required this.sessionName,
    required this.status,
    required this.startTime,
    required this.endTime,
    required this.plannedDurationMinutes,
    required this.mrXRevealInterval,
    required this.teams,
    required this.members,
    required this.latestLocations,
  });

  factory GameSessionSnapshot.fromJson(Map<String, dynamic> json) {
    return GameSessionSnapshot(
      sessionId: _readInt(json, ['sessionId']),
      sessionName: (json['sessionName'] as String?) ?? 'Session',
      status: switch (json['status']) {
        final String s => tryParseSessionStatus(s),
        _ => null,
      },
      startTime: tryParseIsoDateTime(json['startTime']),
      endTime: tryParseIsoDateTime(json['endTime']),
      plannedDurationMinutes: _readInt(json, ['plannedDurationMinutes']),
      mrXRevealInterval: _readInt(json, ['mrXRevealInterval']),
      teams:
          (json['teams'] as List?)
              ?.whereType<Map>()
              .map((e) => SnapshotTeam.fromJson(e.cast<String, dynamic>()))
              .toList(growable: false) ??
          const [],
      members:
          (json['members'] as List?)
              ?.whereType<Map>()
              .map(
                (e) => SnapshotTeamMember.fromJson(e.cast<String, dynamic>()),
              )
              .toList(growable: false) ??
          const [],
      latestLocations:
          (json['latestLocations'] as List?)
              ?.whereType<Map>()
              .map(
                (e) =>
                    SnapshotLatestLocation.fromJson(e.cast<String, dynamic>()),
              )
              .toList(growable: false) ??
          const [],
    );
  }

  GameSessionSnapshot copyWith({
    SessionStatus? status,
    DateTime? startTime,
    DateTime? endTime,
    List<SnapshotTeam>? teams,
    List<SnapshotTeamMember>? members,
    List<SnapshotLatestLocation>? latestLocations,
  }) {
    return GameSessionSnapshot(
      sessionId: sessionId,
      sessionName: sessionName,
      status: status ?? this.status,
      startTime: startTime ?? this.startTime,
      endTime: endTime ?? this.endTime,
      plannedDurationMinutes: plannedDurationMinutes,
      mrXRevealInterval: mrXRevealInterval,
      teams: teams ?? this.teams,
      members: members ?? this.members,
      latestLocations: latestLocations ?? this.latestLocations,
    );
  }
}

final class SnapshotTeam {
  final int id;
  final int sessionId;
  final String teamName;
  final TeamRole? role;
  final String colorCode;
  final bool isCaught;
  final int maxPlayerCount;

  const SnapshotTeam({
    required this.id,
    required this.sessionId,
    required this.teamName,
    required this.role,
    required this.colorCode,
    required this.isCaught,
    required this.maxPlayerCount,
  });

  factory SnapshotTeam.fromJson(Map<String, dynamic> json) {
    return SnapshotTeam(
      id: _readInt(json, ['id']),
      sessionId: _readInt(json, ['sessionId']),
      teamName: (json['teamName'] as String?) ?? 'Team',
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: (json['colorCode'] as String?) ?? '#64748B',
      isCaught: (json['isCaught'] as bool?) ?? false,
      maxPlayerCount: _readInt(json, ['maxPlayerCount']),
    );
  }
}

final class TeamAddedPayload {
  final int teamId;
  final int sessionId;
  final String teamName;
  final TeamRole? role;
  final String colorCode;
  final bool isCaught;
  final int maxPlayerCount;

  const TeamAddedPayload({
    required this.teamId,
    required this.sessionId,
    required this.teamName,
    required this.role,
    required this.colorCode,
    required this.isCaught,
    required this.maxPlayerCount,
  });

  factory TeamAddedPayload.fromJson(Map<String, dynamic> json) {
    return TeamAddedPayload(
      teamId: _readInt(json, ['teamId', 'id']),
      sessionId: _readInt(json, ['sessionId']),
      teamName: (json['teamName'] as String?) ?? 'Team',
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: (json['colorCode'] as String?) ?? '#64748B',
      isCaught: (json['isCaught'] as bool?) ?? false,
      maxPlayerCount: _readInt(json, ['maxPlayerCount']),
    );
  }
}

final class TeamUpdatedPayload {
  final int teamId;
  final int sessionId;
  final String teamName;
  final TeamRole? role;
  final String colorCode;
  final bool isCaught;
  final int maxPlayerCount;

  const TeamUpdatedPayload({
    required this.teamId,
    required this.sessionId,
    required this.teamName,
    required this.role,
    required this.colorCode,
    required this.isCaught,
    required this.maxPlayerCount,
  });

  factory TeamUpdatedPayload.fromJson(Map<String, dynamic> json) {
    return TeamUpdatedPayload(
      teamId: _readInt(json, ['teamId', 'id']),
      sessionId: _readInt(json, ['sessionId']),
      teamName: (json['teamName'] as String?) ?? 'Team',
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: (json['colorCode'] as String?) ?? '#64748B',
      isCaught: (json['isCaught'] as bool?) ?? false,
      maxPlayerCount: _readInt(json, ['maxPlayerCount']),
    );
  }
}

final class TeamDeletedPayload {
  final int teamId;
  final int sessionId;

  const TeamDeletedPayload({required this.teamId, required this.sessionId});

  factory TeamDeletedPayload.fromJson(Map<String, dynamic> json) {
    return TeamDeletedPayload(
      teamId: _readInt(json, ['teamId', 'id']),
      sessionId: _readInt(json, ['sessionId']),
    );
  }
}

final class MrXCaughtPayload {
  final int sessionId;
  final int newMrXTeamId;
  final String newMrXTeamName;
  final int formerMrXTeamId;
  final String formerMrXTeamName;

  const MrXCaughtPayload({
    required this.sessionId,
    required this.newMrXTeamId,
    required this.newMrXTeamName,
    required this.formerMrXTeamId,
    required this.formerMrXTeamName,
  });

  factory MrXCaughtPayload.fromJson(Map<String, dynamic> json) {
    return MrXCaughtPayload(
      sessionId: _readInt(json, ['sessionId']),
      newMrXTeamId: _readInt(json, ['newMrXTeamId']),
      newMrXTeamName: (json['newMrXTeamName'] as String?) ?? 'Team',
      formerMrXTeamId: _readInt(json, ['formerMrXTeamId']),
      formerMrXTeamName: (json['formerMrXTeamName'] as String?) ?? 'Team',
    );
  }
}

final class SnapshotTeamMember {
  final int id;
  final int sessionId;
  final int teamId;
  final String? userId;
  final String? guestName;
  final bool isTeamLeader;
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime? lastUpdated;
  final DateTime? joinedAt;

  const SnapshotTeamMember({
    required this.id,
    required this.sessionId,
    required this.teamId,
    required this.userId,
    required this.guestName,
    required this.isTeamLeader,
    required this.currentLatitude,
    required this.currentLongitude,
    required this.lastUpdated,
    required this.joinedAt,
  });

  factory SnapshotTeamMember.fromJson(Map<String, dynamic> json) {
    return SnapshotTeamMember(
      id: _readInt(json, ['id']),
      sessionId: _readInt(json, ['sessionId']),
      teamId: _readInt(json, ['teamId']),
      userId: json['userId'] as String?,
      guestName: json['guestName'] as String?,
      isTeamLeader: (json['isTeamLeader'] as bool?) ?? false,
      currentLatitude: (json['currentLatitude'] as num?)?.toDouble(),
      currentLongitude: (json['currentLongitude'] as num?)?.toDouble(),
      lastUpdated: tryParseIsoDateTime(json['lastUpdated']),
      joinedAt: tryParseIsoDateTime(json['joinedAt']),
    );
  }

  SnapshotTeamMember copyWith({
    int? teamId,
    String? userId,
    String? guestName,
    bool? isTeamLeader,
    double? currentLatitude,
    double? currentLongitude,
    DateTime? lastUpdated,
  }) {
    return SnapshotTeamMember(
      id: id,
      sessionId: sessionId,
      teamId: teamId ?? this.teamId,
      userId: userId ?? this.userId,
      guestName: guestName ?? this.guestName,
      isTeamLeader: isTeamLeader ?? this.isTeamLeader,
      currentLatitude: currentLatitude ?? this.currentLatitude,
      currentLongitude: currentLongitude ?? this.currentLongitude,
      lastUpdated: lastUpdated ?? this.lastUpdated,
      joinedAt: joinedAt,
    );
  }
}

final class SnapshotLatestLocation {
  final int logId;
  final int memberId;
  final DateTime timestamp;
  final double latitude;
  final double longitude;
  final double accuracyMeters;
  final TransportMode? transportMode;
  final bool isRevealedPosition;

  const SnapshotLatestLocation({
    required this.logId,
    required this.memberId,
    required this.timestamp,
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.transportMode,
    required this.isRevealedPosition,
  });

  factory SnapshotLatestLocation.fromJson(Map<String, dynamic> json) {
    return SnapshotLatestLocation(
      logId: _readInt(json, ['logId', 'id']),
      memberId: _readInt(json, ['memberId']),
      timestamp:
          tryParseIsoDateTime(json['timestamp']) ?? DateTime.now().toUtc(),
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      accuracyMeters: (json['accuracyMeters'] as num).toDouble(),
      transportMode: switch (json['transportMode']) {
        final String s => tryParseTransportMode(s),
        _ => null,
      },
      isRevealedPosition: (json['isRevealedPosition'] as bool?) ?? false,
    );
  }
}

final class TeamMemberJoinedPayload {
  final int memberId;
  final int sessionId;
  final int teamId;
  final String? userId;
  final String? guestName;
  final bool isTeamLeader;
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime? lastUpdated;
  final DateTime? joinedAt;

  const TeamMemberJoinedPayload({
    required this.memberId,
    required this.sessionId,
    required this.teamId,
    required this.userId,
    required this.guestName,
    required this.isTeamLeader,
    required this.currentLatitude,
    required this.currentLongitude,
    required this.lastUpdated,
    required this.joinedAt,
  });

  factory TeamMemberJoinedPayload.fromJson(Map<String, dynamic> json) {
    return TeamMemberJoinedPayload(
      memberId: _readInt(json, ['memberId', 'id']),
      sessionId: _readInt(json, ['sessionId']),
      teamId: _readInt(json, ['teamId']),
      userId: json['userId'] as String?,
      guestName: json['guestName'] as String?,
      isTeamLeader: (json['isTeamLeader'] as bool?) ?? false,
      currentLatitude: (json['currentLatitude'] as num?)?.toDouble(),
      currentLongitude: (json['currentLongitude'] as num?)?.toDouble(),
      lastUpdated: tryParseIsoDateTime(json['lastUpdated']),
      joinedAt: tryParseIsoDateTime(json['joinedAt']),
    );
  }
}

final class TeamMemberUpdatedPayload {
  final int memberId;
  final int sessionId;
  final int teamId;
  final String? userId;
  final String? guestName;
  final bool isTeamLeader;
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime? lastUpdated;

  const TeamMemberUpdatedPayload({
    required this.memberId,
    required this.sessionId,
    required this.teamId,
    required this.userId,
    required this.guestName,
    required this.isTeamLeader,
    required this.currentLatitude,
    required this.currentLongitude,
    required this.lastUpdated,
  });

  factory TeamMemberUpdatedPayload.fromJson(Map<String, dynamic> json) {
    return TeamMemberUpdatedPayload(
      memberId: _readInt(json, ['memberId', 'id']),
      sessionId: _readInt(json, ['sessionId']),
      teamId: _readInt(json, ['teamId']),
      userId: json['userId'] as String?,
      guestName: json['guestName'] as String?,
      isTeamLeader: (json['isTeamLeader'] as bool?) ?? false,
      currentLatitude: (json['currentLatitude'] as num?)?.toDouble(),
      currentLongitude: (json['currentLongitude'] as num?)?.toDouble(),
      lastUpdated: tryParseIsoDateTime(json['lastUpdated']),
    );
  }
}

final class TeamMemberLeftPayload {
  final int memberId;

  const TeamMemberLeftPayload({required this.memberId});

  factory TeamMemberLeftPayload.fromJson(Map<String, dynamic> json) {
    return TeamMemberLeftPayload(memberId: _readInt(json, ['memberId']));
  }
}

final class GameSessionStartedPayload {
  final SessionStatus? status;
  final DateTime? startTime;
  final DateTime? endTime;

  const GameSessionStartedPayload({
    required this.status,
    required this.startTime,
    required this.endTime,
  });

  factory GameSessionStartedPayload.fromJson(Map<String, dynamic> json) {
    return GameSessionStartedPayload(
      status: switch (json['status']) {
        final String s => tryParseSessionStatus(s),
        _ => null,
      },
      startTime: tryParseIsoDateTime(json['startTime']),
      endTime: tryParseIsoDateTime(json['endTime']),
    );
  }
}

final class LocationLogRecordedPayload {
  final int logId;
  final int memberId;
  final DateTime timestamp;
  final double latitude;
  final double longitude;
  final double accuracyMeters;
  final TransportMode? transportMode;
  final bool isRevealedPosition;

  const LocationLogRecordedPayload({
    required this.logId,
    required this.memberId,
    required this.timestamp,
    required this.latitude,
    required this.longitude,
    required this.accuracyMeters,
    required this.transportMode,
    required this.isRevealedPosition,
  });

  factory LocationLogRecordedPayload.fromJson(Map<String, dynamic> json) {
    return LocationLogRecordedPayload(
      logId: _readInt(json, ['logId', 'id']),
      memberId: _readInt(json, ['memberId']),
      timestamp:
          tryParseIsoDateTime(json['timestamp']) ?? DateTime.now().toUtc(),
      latitude: (json['latitude'] as num).toDouble(),
      longitude: (json['longitude'] as num).toDouble(),
      accuracyMeters: (json['accuracyMeters'] as num).toDouble(),
      transportMode: switch (json['transportMode']) {
        final String s => tryParseTransportMode(s),
        _ => null,
      },
      isRevealedPosition: (json['isRevealedPosition'] as bool?) ?? false,
    );
  }
}
