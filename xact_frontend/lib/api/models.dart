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
    'WAITING' => SessionStatus.waiting,
    'ACTIVE' => SessionStatus.active,
    'FINISHED' => SessionStatus.finished,
    _ => null,
  };
}

TeamRole? tryParseTeamRole(String value) {
  return switch (value) {
    'MR_X' => TeamRole.mrX,
    'DETECTIVE' => TeamRole.detective,
    'SPECTATOR' => TeamRole.spectator,
    _ => null,
  };
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
    'FOOT' => TransportMode.foot,
    'BUS' => TransportMode.bus,
    'TRAM' => TransportMode.tram,
    'TRAIN' => TransportMode.train,
    _ => null,
  };
}

PowerUpType? tryParsePowerUpType(String value) {
  return switch (value) {
    'BLACK_TICKET' => PowerUpType.blackTicket,
    'DOUBLE_MOVE' => PowerUpType.doubleMove,
    _ => null,
  };
}

final class UserInfo {
  final int userId;
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
      userId: (json['userId'] as num).toInt(),
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
  final int userId;
  final String username;
  final String email;
  final String passwordHash;
  final AccountType? accountType;
  final DateTime? subscriptionEndDate;
  final int totalWins;
  final int totalGamesPlayed;

  const UserDetails({
    required this.userId,
    required this.username,
    required this.email,
    required this.passwordHash,
    required this.accountType,
    required this.subscriptionEndDate,
    required this.totalWins,
    required this.totalGamesPlayed,
  });

  factory UserDetails.fromJson(Map<String, dynamic> json) {
    return UserDetails(
      userId: (json['userId'] as num).toInt(),
      username: json['username'] as String,
      email: json['email'] as String,
      passwordHash: json['passwordHash'] as String,
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
  final String joinCode;
  final SessionStatus? status;
  final DateTime? startTime;
  final DateTime? endTime;

  const GameSessionInfo({
    required this.sessionId,
    required this.joinCode,
    required this.status,
    required this.startTime,
    required this.endTime,
  });

  factory GameSessionInfo.fromJson(Map<String, dynamic> json) {
    return GameSessionInfo(
      sessionId: (json['sessionId'] as num).toInt(),
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
  final int hostUserId;
  final String joinCode;
  final SessionStatus? status;
  final DateTime? startTime;
  final DateTime? endTime;
  final int plannedDurationMinutes;
  final int mrXRevealInterval;

  const GameSessionDetails({
    required this.sessionId,
    required this.hostUserId,
    required this.joinCode,
    required this.status,
    required this.startTime,
    required this.endTime,
    required this.plannedDurationMinutes,
    required this.mrXRevealInterval,
  });

  factory GameSessionDetails.fromJson(Map<String, dynamic> json) {
    return GameSessionDetails(
      sessionId: (json['sessionId'] as num).toInt(),
      hostUserId: (json['hostUserId'] as num).toInt(),
      joinCode: json['joinCode'] as String,
      status: switch (json['status']) {
        final String s => tryParseSessionStatus(s),
        _ => null,
      },
      startTime: tryParseIsoDateTime(json['startTime']),
      endTime: tryParseIsoDateTime(json['endTime']),
      plannedDurationMinutes: (json['plannedDurationMinutes'] as num).toInt(),
      mrXRevealInterval: (json['mrXRevealInterval'] as num).toInt(),
    );
  }
}

final class TeamInfo {
  final int teamId;
  final String teamName;
  final TeamRole? role;
  final String colorCode;

  const TeamInfo({
    required this.teamId,
    required this.teamName,
    required this.role,
    required this.colorCode,
  });

  factory TeamInfo.fromJson(Map<String, dynamic> json) {
    return TeamInfo(
      teamId: (json['teamId'] as num).toInt(),
      teamName: json['teamName'] as String,
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: json['colorCode'] as String,
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

  const TeamDetails({
    required this.teamId,
    required this.sessionId,
    required this.teamName,
    required this.role,
    required this.colorCode,
    required this.isCaught,
  });

  factory TeamDetails.fromJson(Map<String, dynamic> json) {
    return TeamDetails(
      teamId: (json['teamId'] as num).toInt(),
      sessionId: (json['sessionId'] as num).toInt(),
      teamName: json['teamName'] as String,
      role: switch (json['role']) {
        final String s => tryParseTeamRole(s),
        _ => null,
      },
      colorCode: json['colorCode'] as String,
      isCaught: json['isCaught'] as bool,
    );
  }
}

final class TeamMemberInfo {
  final int memberId;
  final int teamId;
  final int userId;
  final bool isTeamLeader;

  const TeamMemberInfo({
    required this.memberId,
    required this.teamId,
    required this.userId,
    required this.isTeamLeader,
  });

  factory TeamMemberInfo.fromJson(Map<String, dynamic> json) {
    return TeamMemberInfo(
      memberId: (json['memberId'] as num).toInt(),
      teamId: (json['teamId'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      isTeamLeader: json['isTeamLeader'] as bool,
    );
  }
}

final class TeamMemberDetails {
  final int memberId;
  final int teamId;
  final int userId;
  final bool isTeamLeader;
  final double? currentLatitude;
  final double? currentLongitude;
  final DateTime? lastUpdated;

  const TeamMemberDetails({
    required this.memberId,
    required this.teamId,
    required this.userId,
    required this.isTeamLeader,
    required this.currentLatitude,
    required this.currentLongitude,
    required this.lastUpdated,
  });

  factory TeamMemberDetails.fromJson(Map<String, dynamic> json) {
    return TeamMemberDetails(
      memberId: (json['memberId'] as num).toInt(),
      teamId: (json['teamId'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
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
      pointId: (json['pointId'] as num).toInt(),
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
      pointId: (json['pointId'] as num).toInt(),
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
      logId: (json['logId'] as num).toInt(),
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
      logId: (json['logId'] as num).toInt(),
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
      usageId: (json['usageId'] as num).toInt(),
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
      usageId: (json['usageId'] as num).toInt(),
      memberId: (json['memberId'] as num).toInt(),
      powerUpType: switch (json['powerUpType']) {
        final String s => tryParsePowerUpType(s),
        _ => null,
      },
      usedAt: DateTime.parse(json['usedAt'] as String),
    );
  }
}
