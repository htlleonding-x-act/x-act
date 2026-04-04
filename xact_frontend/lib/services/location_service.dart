import 'dart:async';

import 'package:geolocator/geolocator.dart';

import '../api/api_service.dart';

/// Handles GPS acquisition and periodic upload of the current player's
/// position to the backend.
///
/// Usage:
///   1. Call [requestPermission] once (e.g. on lobby join).
///   2. Call [startTracking] when the game begins.
///   3. Listen to [positionStream] to react to position changes in the UI.
///   4. Call [stopTracking] when leaving the game screen.
final class LocationService {
  LocationService._();
  static final LocationService instance = LocationService._();

  // ── Member info set when tracking starts ──────────────────────────────────

  int? _memberId;
  int? _sessionId;
  int? _teamId;
  int? _userId;
  bool _isTeamLeader = false;

  // ── GPS subscription & upload timer ───────────────────────────────────────

  StreamSubscription<Position>? _positionSub;
  Timer? _uploadTimer;

  // ── Position broadcast stream ─────────────────────────────────────────────

  final _positionController = StreamController<Position>.broadcast();

  /// Emits every time a new GPS position is received.
  Stream<Position> get positionStream => _positionController.stream;

  /// The most recently received GPS position, or `null` before tracking starts.
  Position? lastKnownPosition;

  // ── Public API ────────────────────────────────────────────────────────────

  /// Returns `true` when the app already has location permission.
  Future<bool> hasPermission() async {
    final perm = await Geolocator.checkPermission();
    return perm == LocationPermission.whileInUse ||
        perm == LocationPermission.always;
  }

  /// Requests location permission from the OS.
  /// Returns `true` if the user grants it (whileInUse or always).
  Future<bool> requestPermission() async {
    LocationPermission permission = await Geolocator.checkPermission();

    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.deniedForever) {
      // User blocked it permanently – open app settings so they can fix it.
      await Geolocator.openAppSettings();
      return false;
    }

    return permission == LocationPermission.whileInUse ||
        permission == LocationPermission.always;
  }

  /// Starts watching the device position and feeding [positionStream] without
  /// uploading anything to the backend. Use this when you just need the map to
  /// show the player's real position (e.g. before the game starts / no login
  /// yet). Calling [startTracking] later will replace this subscription.
  Future<void> startWatching() async {
    if (_positionSub != null) return; // already running

    final granted = await requestPermission();
    if (!granted) return;

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 5,
    );

    _positionSub =
        Geolocator.getPositionStream(locationSettings: locationSettings).listen(
          (position) {
            lastKnownPosition = position;
            _positionController.add(position);
          },
          onError: (_) {},
        );
  }

  /// Starts continuous GPS tracking and periodic upload to the backend.
  ///
  /// [memberId], [teamId], [userId] and [isTeamLeader] must match the current
  /// player's `TeamMember` record in the backend.
  ///
  /// [uploadInterval] controls how often the position is pushed to the API
  /// (default: every 5 seconds).
  Future<void> startTracking({
    required int sessionId,
    required int memberId,
    required int teamId,
    required int userId,
    required bool isTeamLeader,
    Duration uploadInterval = const Duration(seconds: 5),
  }) async {
    // If already tracking, stop first.
    stopTracking();

    _memberId = memberId;
    _sessionId = sessionId;
    _teamId = teamId;
    _userId = userId;
    _isTeamLeader = isTeamLeader;

    final granted = await requestPermission();
    if (!granted) return;

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      // Only emit a new event when the device has moved at least 5 m.
      distanceFilter: 5,
    );

    _positionSub =
        Geolocator.getPositionStream(locationSettings: locationSettings).listen(
          (position) {
            lastKnownPosition = position;
            _positionController.add(position);
          },
          onError: (Object error) {
            // Swallow errors so the stream stays alive.
          },
        );

    // Upload on a fixed interval so the backend is always up-to-date even
    // when the player is standing still (distanceFilter would skip those).
    _uploadTimer = Timer.periodic(uploadInterval, (_) => _uploadPosition());
  }

  /// Stops GPS tracking and cancels uploads.
  void stopTracking() {
    _positionSub?.cancel();
    _uploadTimer?.cancel();
    _positionSub = null;
    _uploadTimer = null;
  }

  /// Call when the service is no longer needed (e.g. app shutdown).
  void dispose() {
    stopTracking();
    _positionController.close();
  }

  // ── Private helpers ───────────────────────────────────────────────────────

  Future<void> _uploadPosition() async {
    final position = lastKnownPosition;
    final sessionId = _sessionId;
    final memberId = _memberId;
    final teamId = _teamId;
    final userId = _userId;

    if (position == null ||
        sessionId == null ||
        memberId == null ||
        teamId == null ||
        userId == null) {
      return;
    }

    try {
      await ApiService.instance.updateTeamMemberLocation(
        sessionId: sessionId,
        memberId: memberId,
        teamId: teamId,
        userId: userId,
        isTeamLeader: _isTeamLeader,
        latitude: position.latitude,
        longitude: position.longitude,
      );
    } catch (_) {
      // Don't crash – network may be temporarily unavailable.
    }
  }
}
