import 'dart:async';

import 'package:geolocator/geolocator.dart';

import '../api/api_service.dart';

/// call [requestPermission] once, e.g. when joining a lobby, then
/// [startTracking] when the game begins and [stopTracking] when leaving the
/// game screen. the ui listens to [positionStream]
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

  final _positionController = StreamController<Position>.broadcast();

  Stream<Position> get positionStream => _positionController.stream;

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
    if (!granted) {
      return;
    }

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
  /// backend
  Future<void> startTracking({
    required int sessionId,
    required int memberId,
    required int teamId,
    Duration uploadInterval = const Duration(seconds: 5),
  }) async {
    stopTracking();
    final generation = _generation;

    _memberId = memberId;
    _sessionId = sessionId;
    _teamId = teamId;

    final granted = await requestPermission();
    if (!granted) {
      return;
    }

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
    // player stands still, which the distanceFilter would skip
    _uploadTimer = Timer.periodic(uploadInterval, (_) => _uploadPosition());
  }

  void stopTracking() {
    _generation++;
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
    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      // in meters
      distanceFilter: 5,
    );

    _positionSub ??=
        Geolocator.getPositionStream(locationSettings: locationSettings).listen(
          (position) {
            lastKnownPosition = position;
            _positionController.add(position);
          },
          onError: (_) {},
        );
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
    } catch (_) {
      // the network may be gone for a moment, the next tick tries again
    }
  }
}
