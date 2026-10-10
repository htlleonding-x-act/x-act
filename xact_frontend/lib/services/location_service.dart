import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:geolocator/geolocator.dart';

import '../api/api_service.dart';

/// call [startWatching] when entering a lobby, [startTracking] when the game
/// begins and [stopTracking] when the player leaves or the match ends. on
/// android the position stream runs as a foreground service, so it keeps
/// going while the phone is locked. the ui listens to [positionStream]
final class LocationService {
  LocationService._();
  static final LocationService instance = LocationService._();

  int? _memberId;
  int? _sessionId;
  int? _teamId;

  StreamSubscription<Position>? _positionSub;
  Timer? _uploadTimer;
  // stopTracking() bumps this. a start call still waiting for permission or
  // the last known position sees the change and gives up, so it never leaves a
  // gps stream or upload timer running that nothing would cancel
  int _generation = 0;
  // set while a lobby or match needs positions, so [resume] knows whether to
  // start a stream that could not start in the background
  bool _wanted = false;

  final _positionController = StreamController<Position>.broadcast();

  Stream<Position> get positionStream => _positionController.stream;

  final _ownRevealController = StreamController<void>.broadcast();

  /// fires when the server revealed one of this player's pings, which only
  /// happens to mister x
  Stream<void> get ownPositionRevealed => _ownRevealController.stream;

  bool get isTracking => _uploadTimer != null;

  Position? lastKnownPosition;

  /// true when the user grants whileInUse or always
  Future<bool> requestPermission() async {
    LocationPermission permission = await Geolocator.checkPermission();

    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.deniedForever) {
      // blocked for good, so open the app settings where the user can change it
      await Geolocator.openAppSettings();
      return false;
    }

    return permission == LocationPermission.whileInUse ||
        permission == LocationPermission.always;
  }

  /// true when whileInUse or always is already granted. unlike
  /// [requestPermission] it never prompts or opens settings
  Future<bool> hasPermission() async {
    final permission = await Geolocator.checkPermission();
    return permission == LocationPermission.whileInUse ||
        permission == LocationPermission.always;
  }

  /// a one-shot gps fix, or null when permission is denied, location services
  /// are off or the lookup fails or times out. it never throws, so callers
  /// treat null as a signal to use a fallback location
  Future<Position?> getCurrentPosition({
    Duration timeLimit = const Duration(seconds: 10),
  }) async {
    try {
      final granted = await requestPermission();
      if (!granted) {
        return null;
      }

      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        final lastKnown = await Geolocator.getLastKnownPosition();
        if (lastKnown != null) {
          lastKnownPosition = lastKnown;
          _positionController.add(lastKnown);
        }
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
      return position;
    } catch (_) {
      // timeout or platform error, fall back to the last known fix if there is one
      try {
        final lastKnown = await Geolocator.getLastKnownPosition();
        if (lastKnown != null) {
          lastKnownPosition = lastKnown;
          _positionController.add(lastKnown);
        }
        return lastKnown;
      } catch (_) {
        return null;
      }
    }
  }

  /// feeds [positionStream] without uploading anything, for when the map only
  /// needs to show the real position, e.g. before the game starts. a later
  /// [startTracking] replaces this subscription
  Future<void> startWatching() async {
    if (_positionSub != null) return;
    final generation = _generation;

    final granted = await requestPermission();
    if (!granted || generation != _generation) {
      return;
    }
    _wanted = true;

    // send the last known location right away so the ui doesn't wait for a fresh fix
    try {
      final lastKnown = await Geolocator.getLastKnownPosition();
      if (lastKnown != null) {
        lastKnownPosition = lastKnown;
        _positionController.add(lastKnown);
      }
    } catch (_) {}

    if (generation != _generation) return;

    // force a first fix, some devices otherwise stay stuck on acquiring gps
    unawaited(getCurrentPosition(timeLimit: const Duration(seconds: 5)));

    _listenToPositions();
  }

  /// [memberId] and [teamId] must match the player's `TeamMember` in the
  /// backend. a stream started in the lobby keeps running, because android
  /// refuses to start the foreground service again while the phone is locked
  Future<void> startTracking({
    required int sessionId,
    required int memberId,
    required int teamId,
    Duration uploadInterval = const Duration(seconds: 5),
  }) async {
    final generation = _generation;

    _memberId = memberId;
    _sessionId = sessionId;
    _teamId = teamId;

    final granted = await requestPermission();
    if (!granted || generation != _generation) {
      return;
    }
    _wanted = true;

    // send the last known location right away so the ui doesn't wait for a fresh fix
    try {
      final lastKnown = await Geolocator.getLastKnownPosition();
      if (lastKnown != null) {
        lastKnownPosition = lastKnown;
        _positionController.add(lastKnown);
      }
    } catch (_) {}

    if (generation != _generation) return;

    // force a first fix, some devices otherwise stay stuck on acquiring gps
    unawaited(getCurrentPosition(timeLimit: const Duration(seconds: 5)));

    _listenToPositions();

    // upload on a fixed interval so the backend stays current even while the
    // player stands still, which the distanceFilter would skip. cancel first
    // so two overlapping calls don't leave a timer running
    _uploadTimer?.cancel();
    _uploadTimer = Timer.periodic(uploadInterval, (_) => _uploadPosition());
  }

  /// starts the stream again if it could not start while the app was in the
  /// background. call it when the app becomes visible
  void resume() {
    if (_wanted) {
      _listenToPositions();
    }
  }

  void stopTracking() {
    _generation++;
    _wanted = false;
    _positionSub?.cancel();
    _uploadTimer?.cancel();
    _positionSub = null;
    _uploadTimer = null;
  }

  void dispose() {
    stopTracking();
    _positionController.close();
  }

  void _listenToPositions() {
    // android only starts a foreground service while the app is visible. a
    // refused start fails silently and leaves a stream that never delivers,
    // so wait for [resume] instead
    final state = WidgetsBinding.instance.lifecycleState;
    if (state == AppLifecycleState.hidden ||
        state == AppLifecycleState.paused) {
      return;
    }

    _positionSub ??=
        Geolocator.getPositionStream(
          locationSettings: _streamSettings(),
        ).listen((position) {
          lastKnownPosition = position;
          _positionController.add(position);
        }, onError: (_) {});
  }

  LocationSettings _streamSettings() {
    // in meters
    const distanceFilter = 5;

    if (kIsWeb) {
      return const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: distanceFilter,
      );
    }

    return switch (defaultTargetPlatform) {
      TargetPlatform.android => AndroidSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: distanceFilter,
        foregroundNotificationConfig: const ForegroundNotificationConfig(
          notificationTitle: 'X-ACT is running',
          notificationText:
              'Your position stays shared while the screen is off',
          notificationChannelName: 'Game tracking',
          // keeps the cpu awake so the upload timer still fires with the
          // screen off, otherwise the pings stop while the player stands still
          enableWakeLock: true,
          setOngoing: true,
        ),
      ),
      TargetPlatform.iOS => AppleSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: distanceFilter,
        activityType: ActivityType.fitness,
        pauseLocationUpdatesAutomatically: false,
        allowBackgroundLocationUpdates: true,
        showBackgroundLocationIndicator: true,
      ),
      _ => const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: distanceFilter,
      ),
    };
  }

  Future<void> _uploadPosition() async {
    final position = lastKnownPosition;
    final sessionId = _sessionId;
    final memberId = _memberId;
    final teamId = _teamId;

    if (position == null ||
        sessionId == null ||
        memberId == null ||
        teamId == null) {
      return;
    }

    try {
      final revealed = await ApiService.instance.addLocationLog(
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
      if (revealed) {
        _ownRevealController.add(null);
      }
    } catch (_) {
      // the network may be gone for a moment, the next tick tries again
    }
  }
}
