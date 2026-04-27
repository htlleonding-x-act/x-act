import 'dart:async';

import 'package:flutter/foundation.dart';
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

  // ── GPS subscription & upload timer ───────────────────────────────────────

  StreamSubscription<Position>? _positionSub;
  Timer? _uploadTimer;

  // ── Position broadcast stream ─────────────────────────────────────────────

  final _positionController = StreamController<Position>.broadcast();

  /// Emits every time a new GPS position is received.
  Stream<Position> get positionStream => _positionController.stream;

  bool get isTracking => _uploadTimer != null;

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
    //TODO: Remove Logger
    //leaving logger in case it happens again
    _log('LocationService: current permission = $permission');

    if (permission == LocationPermission.denied) {
      _log('LocationService: requesting location permission');
      permission = await Geolocator.requestPermission();
      _log('LocationService: permission result = $permission');
    }

    if (permission == LocationPermission.deniedForever) {
      _log('LocationService: permission denied forever, opening app settings');
      // User blocked it permanently – open app settings so they can fix it.
      await Geolocator.openAppSettings();
      return false;
    }

    return permission == LocationPermission.whileInUse ||
        permission == LocationPermission.always;
  }

  /// Returns a one-shot GPS fix, or `null` when permissions are denied,
  /// location services are off, or the lookup fails / times out.
  ///
  /// Callers should treat `null` as "use a fallback location" – this method
  /// never throws.
  Future<Position?> getCurrentPosition({
    Duration timeLimit = const Duration(seconds: 10),
  }) async {
    _log('LocationService: getCurrentPosition start (timeout: $timeLimit)');
    try {
      final granted = await requestPermission();
      if (!granted) {
        _log(
          'LocationService: getCurrentPosition aborted - permission not granted',
        );
        return null;
      }

      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      _log('LocationService: location service enabled = $serviceEnabled');
      if (!serviceEnabled) {
        final lastKnown = await Geolocator.getLastKnownPosition();
        _log(
          'LocationService: returning last known position because service is disabled: ${lastKnown != null}',
        );
        return lastKnown;
      }

      final position = await Geolocator.getCurrentPosition(
        locationSettings: LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: timeLimit,
        ),
      );
      lastKnownPosition = position;
      _positionController.add(position);
      _log(
        'LocationService: getCurrentPosition success lat=${position.latitude} lon=${position.longitude} acc=${position.accuracy}',
      );
      return position;
    } catch (_) {
      _log(
        'LocationService: getCurrentPosition failed, falling back to last known position',
      );
      // Timeout or platform error – fall back to last known fix if we have one.
      try {
        final lastKnown = await Geolocator.getLastKnownPosition();
        _log(
          'LocationService: last known position available = ${lastKnown != null}',
        );
        return lastKnown;
      } catch (_) {
        _log('LocationService: last known position lookup failed');
        return null;
      }
    }
  }

  /// Starts watching the device position and feeding [positionStream] without
  /// uploading anything to the backend. Use this when you just need the map to
  /// show the player's real position (e.g. before the game starts / no login
  /// yet). Calling [startTracking] later will replace this subscription.
  Future<void> startWatching() async {
    if (_positionSub != null) return; // already running

    _log('LocationService: startWatching called');
    final granted = await requestPermission();
    if (!granted) {
      _log('LocationService: startWatching aborted - permission not granted');
      return;
    }

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 5,
    );

    _log('LocationService: subscribing to position stream');
    _positionSub =
        Geolocator.getPositionStream(locationSettings: locationSettings).listen(
          (position) {
            lastKnownPosition = position;
            _positionController.add(position);
            _log(
              'LocationService: stream position lat=${position.latitude} lon=${position.longitude} acc=${position.accuracy}',
            );
          },
          onError: (Object error) {
            _log('LocationService: position stream error: $error');
          },
        );

    // Some devices take a while before the first stream event arrives.
    // Prime the UI with a one-shot fix so the map does not stay stuck on the
    // loading indicator if the stream is slow to emit.
    if (lastKnownPosition == null) {
      final primedPosition = await getCurrentPosition(
        timeLimit: const Duration(seconds: 5),
      );
      if (primedPosition != null) {
        lastKnownPosition = primedPosition;
      }
    }
  }

  /// Starts continuous GPS tracking and periodic upload to the backend.
  ///
  /// [memberId] and [teamId] must match the current player's `TeamMember`
  /// record in the backend.
  ///
  /// [uploadInterval] controls how often the position is pushed to the API
  /// (default: every 5 seconds).
  Future<void> startTracking({
    required int sessionId,
    required int memberId,
    required int teamId,
    Duration uploadInterval = const Duration(seconds: 5),
  }) async {
    // If already tracking, stop first.
    stopTracking();

    _log(
      'LocationService: startTracking session=$sessionId member=$memberId team=$teamId interval=$uploadInterval',
    );

    _memberId = memberId;
    _sessionId = sessionId;
    _teamId = teamId;

    final granted = await requestPermission();
    if (!granted) {
      _log('LocationService: startTracking aborted - permission not granted');
      return;
    }

    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      // Only emit a new event when the device has moved at least 5 m.
      distanceFilter: 5,
    );

    _log('LocationService: subscribing to tracking stream');
    _positionSub =
        Geolocator.getPositionStream(locationSettings: locationSettings).listen(
          (position) {
            lastKnownPosition = position;
            _positionController.add(position);
            _log(
              'LocationService: tracking position lat=${position.latitude} lon=${position.longitude} acc=${position.accuracy}',
            );
          },
          onError: (Object error) {
            _log('LocationService: tracking stream error: $error');
            // Swallow errors so the stream stays alive.
          },
        );

    if (lastKnownPosition == null) {
      final primedPosition = await getCurrentPosition(
        timeLimit: const Duration(seconds: 5),
      );
      if (primedPosition != null) {
        lastKnownPosition = primedPosition;
      }
    }

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

    if (position == null ||
        sessionId == null ||
        memberId == null ||
        teamId == null) {
      _log(
        'LocationService: upload skipped (position=${position != null}, session=${sessionId != null}, member=${memberId != null}, team=${teamId != null})',
      );
      return;
    }

    try {
      await ApiService.instance.addLocationLog(
        sessionId: sessionId,
        teamId: teamId,
        memberId: memberId,
        timestamp: DateTime.now().toUtc(),
        latitude: position.latitude,
        longitude: position.longitude,
        accuracyMeters: position.accuracy,
        transportMode: 'Foot',
        isRevealedPosition: false,
      );
      _log(
        'LocationService: uploaded position member=$memberId lat=${position.latitude} lon=${position.longitude}',
      );
    } catch (_) {
      _log('LocationService: upload failed');
      // Don't crash – network may be temporarily unavailable.
    }
  }

  /// Simple debug logger that only prints in debug builds.
  static void _log(String message) {
    if (kDebugMode) debugPrint(message);
  }
}
