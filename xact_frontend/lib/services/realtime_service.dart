import 'dart:async';

import 'package:signalr_netcore/iretry_policy.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../api/models.dart';

final class RealtimeService {
  RealtimeService._();

  static final RealtimeService instance = RealtimeService._();

  HubConnection? _connection;
  String? _hubUrl;
  Future<void>? _connectInFlight;
  int? _subscribedSessionId;
  ({int sessionId, int teamId})? _joinedTeamChannel;
  List<Object>? _presenceArgs;

  GameSessionSnapshot? _latestSnapshot;

  final StreamController<RealtimeEventEnvelope> _eventController =
      StreamController<RealtimeEventEnvelope>.broadcast();
  final StreamController<GameSessionSnapshot> _snapshotController =
      StreamController<GameSessionSnapshot>.broadcast();

  Stream<RealtimeEventEnvelope> get eventStream => _eventController.stream;
  Stream<GameSessionSnapshot> get snapshotStream => _snapshotController.stream;

  GameSessionSnapshot? get latestSnapshot => _latestSnapshot;

  bool get isConnected =>
      _connection?.state == HubConnectionState.Connected;

  bool get _isReconnecting =>
      _connection?.state == HubConnectionState.Reconnecting;

  Future<void> connect({required String baseUrl}) async {
    final hubUrl = Uri.parse(baseUrl).resolve('/hubs/game-session').toString();

    if (_connection != null &&
        _hubUrl == hubUrl &&
        (isConnected || _isReconnecting)) {
      return;
    }

    final inFlight = _connectInFlight;
    if (inFlight != null) {
      return inFlight;
    }

    final attempt = _connect(hubUrl);
    _connectInFlight = attempt;
    try {
      await attempt;
    } finally {
      _connectInFlight = null;
    }
  }

  Future<void> _connect(String hubUrl) async {
    await disconnect();

    final connection = HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect(reconnectPolicy: const _RetryForeverPolicy())
        .build();

    connection.onreconnected(({connectionId}) {
      unawaited(_restoreAfterReconnect());
    });

    connection.on(RealtimeMethods.event, (arguments) {
      if (arguments == null || arguments.isEmpty || arguments.first == null) {
        return;
      }

      final raw = arguments.first;
      if (raw is! Map) {
        return;
      }

      final envelope = RealtimeEventEnvelope.fromJson(
        raw.cast<String, dynamic>(),
      );
      _eventController.add(envelope);
      _applyEvent(envelope);
    });

    connection.on(RealtimeMethods.snapshot, (arguments) {
      if (arguments == null || arguments.isEmpty || arguments.first == null) {
        return;
      }

      final raw = arguments.first;
      if (raw is! Map) {
        return;
      }

      final snapshot = GameSessionSnapshot.fromJson(raw.cast<String, dynamic>());
      _latestSnapshot = snapshot;
      _snapshotController.add(snapshot);
    });

    await connection.start();

    _connection = connection;
    _hubUrl = hubUrl;
  }

  Future<GameSessionSnapshot?> subscribeSession(int sessionId) async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return null;
    }

    final result = await connection.invoke(
      'SubscribeSession',
      args: [sessionId],
    );

    if (result is! Map) {
      return null;
    }

    final snapshot = GameSessionSnapshot.fromJson(result.cast<String, dynamic>());
    _latestSnapshot = snapshot;
    _subscribedSessionId = sessionId;
    _snapshotController.add(snapshot);
    return snapshot;
  }

  Future<GameSessionSnapshot?> requestSnapshot(int sessionId) async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return null;
    }

    final result = await connection.invoke(
      'RequestSnapshot',
      args: [sessionId],
    );

    if (result is! Map) {
      return null;
    }

    final snapshot = GameSessionSnapshot.fromJson(result.cast<String, dynamic>());
    _latestSnapshot = snapshot;
    _snapshotController.add(snapshot);
    return snapshot;
  }

  Future<void> unsubscribeSession(int sessionId) async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return;
    }

    await connection.invoke('UnsubscribeSession', args: [sessionId]);
    if (_subscribedSessionId == sessionId) {
      _subscribedSessionId = null;
      _latestSnapshot = null;
    }
  }

  Future<void> joinTeamChannel({
    required int sessionId,
    required int teamId,
  }) async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return;
    }

    await connection.invoke('JoinTeamChannel', args: [sessionId, teamId]);
    _joinedTeamChannel = (sessionId: sessionId, teamId: teamId);
  }

  Future<void> leaveTeamChannel({
    required int sessionId,
    required int teamId,
  }) async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return;
    }

    await connection.invoke('LeaveTeamChannel', args: [sessionId, teamId]);
    if (_joinedTeamChannel == (sessionId: sessionId, teamId: teamId)) {
      _joinedTeamChannel = null;
    }
  }

  Future<void> registerMemberPresence({
    required int sessionId,
    required int teamId,
    required int memberId,
    int? userId,
    String? guestName,
  }) async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return;
    }

    // signalr_netcore needs non-null Object entries in args
    final args = <Object>[
      sessionId,
      teamId,
      memberId,
      userId ?? 0,
      guestName ?? '',
    ];
    await connection.invoke('RegisterMemberPresence', args: args);
    _presenceArgs = args;
  }

  Future<void> unregisterMemberPresence() async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return;
    }

    await connection.invoke('UnregisterMemberPresence');
    _presenceArgs = null;
  }

  Future<void> disconnect() async {
    final connection = _connection;
    _connection = null;
    _hubUrl = null;
    _subscribedSessionId = null;
    _joinedTeamChannel = null;
    _presenceArgs = null;
    _latestSnapshot = null;

    if (connection != null) {
      await connection.stop();
    }
  }

  /// the server treats a reconnect as a new connection without groups or
  /// presence, so restore both. resubscribing also refetches missed state.
  /// errors are ignored because the next reconnect or refresh tries again
  Future<void> _restoreAfterReconnect() async {
    try {
      final sessionId = _subscribedSessionId;
      if (sessionId != null) {
        await subscribeSession(sessionId);
      }

      final teamChannel = _joinedTeamChannel;
      if (teamChannel != null) {
        await joinTeamChannel(
          sessionId: teamChannel.sessionId,
          teamId: teamChannel.teamId,
        );
      }

      final presenceArgs = _presenceArgs;
      if (presenceArgs != null) {
        await _connection?.invoke('RegisterMemberPresence', args: presenceArgs);
      }
    } catch (_) {}
  }

  void _applyEvent(RealtimeEventEnvelope envelope) {
    final snapshot = _latestSnapshot;
    if (snapshot == null) {
      return;
    }

    switch (envelope.type) {
      case RealtimeEvents.teamAdded:
        final payload = TeamAddedPayload.fromJson(envelope.payload);
        final teams = List<SnapshotTeam>.of(snapshot.teams)
          ..removeWhere((team) => team.id == payload.teamId)
          ..add(
            SnapshotTeam(
              id: payload.teamId,
              sessionId: payload.sessionId,
              teamName: payload.teamName,
              role: payload.role,
              colorCode: payload.colorCode,
              isCaught: payload.isCaught,
              maxPlayerCount: payload.maxPlayerCount,
            ),
          );

        _latestSnapshot = snapshot.copyWith(teams: teams);
        break;

      case RealtimeEvents.teamUpdated:
        final payload = TeamUpdatedPayload.fromJson(envelope.payload);
        final teams = snapshot.teams
            .map((team) {
              if (team.id != payload.teamId) {
                return team;
              }

              return SnapshotTeam(
                id: payload.teamId,
                sessionId: payload.sessionId,
                teamName: payload.teamName,
                role: payload.role,
                colorCode: payload.colorCode,
                isCaught: payload.isCaught,
                maxPlayerCount: payload.maxPlayerCount,
              );
            })
            .toList(growable: false);

        _latestSnapshot = snapshot.copyWith(teams: teams);
        break;

      case RealtimeEvents.teamDeleted:
        final payload = TeamDeletedPayload.fromJson(envelope.payload);
        final teams = snapshot.teams
            .where((team) => team.id != payload.teamId)
            .toList(growable: false);
        final members = snapshot.members
            .where((member) => member.teamId != payload.teamId)
            .toList(growable: false);

        _latestSnapshot = snapshot.copyWith(teams: teams, members: members);
        break;

      case RealtimeEvents.teamMemberJoined:
        final payload = TeamMemberJoinedPayload.fromJson(envelope.payload);
        final members = List<SnapshotTeamMember>.of(snapshot.members)
          ..removeWhere((member) => member.id == payload.memberId)
          ..add(
            SnapshotTeamMember(
              id: payload.memberId,
              sessionId: payload.sessionId,
              teamId: payload.teamId,
              userId: payload.userId,
              guestName: payload.guestName,
              isTeamLeader: payload.isTeamLeader,
              currentLatitude: payload.currentLatitude,
              currentLongitude: payload.currentLongitude,
              lastUpdated: payload.lastUpdated,
              joinedAt: payload.joinedAt,
            ),
          );

        _latestSnapshot = snapshot.copyWith(members: members);
        break;

      case RealtimeEvents.teamMemberUpdated:
        final payload = TeamMemberUpdatedPayload.fromJson(envelope.payload);
        final members = snapshot.members
            .map((member) {
              if (member.id != payload.memberId) {
                return member;
              }

              return member.copyWith(
                teamId: payload.teamId,
                userId: payload.userId,
                guestName: payload.guestName,
                isTeamLeader: payload.isTeamLeader,
                currentLatitude: payload.currentLatitude,
                currentLongitude: payload.currentLongitude,
                lastUpdated: payload.lastUpdated,
              );
            })
            .toList(growable: false);

        _latestSnapshot = snapshot.copyWith(members: members);
        break;

      case RealtimeEvents.teamMemberLeft:
        final payload = TeamMemberLeftPayload.fromJson(envelope.payload);
        final members = snapshot.members
            .where((member) => member.id != payload.memberId)
            .toList(growable: false);
        final latestLocations = snapshot.latestLocations
            .where((location) => location.memberId != payload.memberId)
            .toList(growable: false);

        _latestSnapshot = snapshot.copyWith(
          members: members,
          latestLocations: latestLocations,
        );
        break;

      case RealtimeEvents.gameSessionStarted:
        final payload = GameSessionStartedPayload.fromJson(envelope.payload);
        _latestSnapshot = snapshot.copyWith(
          status: payload.status,
          startTime: payload.startTime,
          endTime: payload.endTime,
        );
        break;

      case RealtimeEvents.gameSessionEnded:
        final payload = GameSessionEndedPayload.fromJson(envelope.payload);
        _latestSnapshot = snapshot.copyWith(
          status: payload.status,
          startTime: payload.startTime,
          endTime: payload.endTime,
        );
        break;

      case RealtimeEvents.locationLogRecorded:
        final payload = LocationLogRecordedPayload.fromJson(envelope.payload);

        SnapshotTeamMember? targetMember;
        for (final member in snapshot.members) {
          if (member.id == payload.memberId) {
            targetMember = member;
            break;
          }
        }

        SnapshotTeam? targetTeam;
        if (targetMember != null) {
          for (final team in snapshot.teams) {
            if (team.id == targetMember.teamId) {
              targetTeam = team;
              break;
            }
          }
        }
        final isMisterX = targetTeam?.role == TeamRole.mrX;

        // keep the last reveal ping on the map between intervals
        if (isMisterX && !payload.isRevealedPosition) {
          _latestSnapshot = snapshot;
          break;
        }

        final latestLocations = List<SnapshotLatestLocation>.of(
          snapshot.latestLocations,
        )..removeWhere((location) => location.memberId == payload.memberId);

        latestLocations.add(
          SnapshotLatestLocation(
            logId: payload.logId,
            memberId: payload.memberId,
            timestamp: payload.timestamp,
            latitude: payload.latitude,
            longitude: payload.longitude,
            accuracyMeters: payload.accuracyMeters,
            transportMode: payload.transportMode,
            isRevealedPosition: payload.isRevealedPosition,
          ),
        );

        final members = snapshot.members
            .map((member) {
              if (member.id != payload.memberId) {
                return member;
              }

              return member.copyWith(
                currentLatitude: payload.latitude,
                currentLongitude: payload.longitude,
                lastUpdated: payload.timestamp,
              );
            })
            .toList(growable: false);

        _latestSnapshot = snapshot.copyWith(
          members: members,
          latestLocations: latestLocations,
        );
        break;

      default:
        return;
    }

    final updated = _latestSnapshot;
    if (updated != null) {
      _snapshotController.add(updated);
    }
  }
}

/// signalr's default policy gives up after four attempts (about 42 s) and
/// leaves the connection closed. a match can last an hour on flaky mobile
/// networks, so retry quickly at first, then every 10 s, and never stop
final class _RetryForeverPolicy implements IRetryPolicy {
  const _RetryForeverPolicy();

  static const _initialDelaysMs = [0, 2000, 5000];

  @override
  int? nextRetryDelayInMilliseconds(RetryContext retryContext) {
    final attempt = retryContext.previousRetryCount;
    return attempt < _initialDelaysMs.length
        ? _initialDelaysMs[attempt]
        : 10000;
  }
}
