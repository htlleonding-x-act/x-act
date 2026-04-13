import 'dart:async';

import 'package:signalr_netcore/signalr_client.dart';

import '../api/models.dart';

final class RealtimeService {
  RealtimeService._();

  static final RealtimeService instance = RealtimeService._();

  HubConnection? _connection;
  String? _hubUrl;
  int? _subscribedSessionId;

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

  Future<void> connect({required String baseUrl}) async {
    final hubUrl = Uri.parse(baseUrl).resolve('/hubs/game-session').toString();

    if (_connection != null && _hubUrl == hubUrl && isConnected) {
      return;
    }

    await disconnect();

    final connection = HubConnectionBuilder().withUrl(hubUrl).build();

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

    await connection.invoke(
      'RegisterMemberPresence',
      // signalr_netcore expects non-null Object entries in args.
      args: [
        sessionId,
        teamId,
        memberId,
        userId ?? 0,
        guestName ?? '',
      ],
    );
  }

  Future<void> unregisterMemberPresence() async {
    final connection = _connection;
    if (connection == null || !isConnected) {
      return;
    }

    await connection.invoke('UnregisterMemberPresence');
  }

  Future<void> disconnect() async {
    final connection = _connection;
    _connection = null;
    _hubUrl = null;
    _subscribedSessionId = null;
    _latestSnapshot = null;

    if (connection != null) {
      await connection.stop();
    }
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

      case RealtimeEvents.locationLogRecorded:
        final payload = LocationLogRecordedPayload.fromJson(envelope.payload);

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
