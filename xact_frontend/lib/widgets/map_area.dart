import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';
import '../api/api_service.dart';
import '../services/location_service.dart';
import 'map_header.dart';
import 'map_legend.dart';

class MapArea extends StatefulWidget {
  const MapArea({super.key});

  @override
  State<MapArea> createState() => _MapAreaState();
}

class _MapAreaState extends State<MapArea> {
  final MapController _mapController = MapController();

  // Fallback center (HTL Leonding) – overridden as soon as GPS kicks in.
  static const LatLng _fallbackCenter = LatLng(48.3069, 14.2858);

  // Current GPS position of "this" player. Null until first fix.
  LatLng? _myPosition;

  // When true, the map auto-pans to follow the player's GPS position.
  bool _followMode = true;

  StreamSubscription<Position>? _positionSub;

  // ── Geofence ──────────────────────────────────────────────────────────────
  // Polygon boundary loaded from the backend for the active session.
  List<LatLng> _geofencePoints = [];
  // True when the player is outside the defined game area.
  bool _isOutOfBounds = false;

  // ── Placeholder teammates (replaced by real API data in a future task) ──
  final List<PlayerMarker> _otherPlayers = [
    PlayerMarker(
      id: 'player2',
      name: 'Team Member 1',
      position: const LatLng(48.3089, 14.2898),
      color: Colors.green,
    ),
    PlayerMarker(
      id: 'player3',
      name: 'Team Member 2',
      position: const LatLng(48.3049, 14.2838),
      color: Colors.green,
    ),
    PlayerMarker(
      id: 'misterx',
      name: 'Mister X (Last Ping)',
      position: const LatLng(48.3107, 14.2820),
      color: Colors.red,
      isMisterX: true,
    ),
  ];

  @override
  void initState() {
    super.initState();
    _startListeningToGps();
    _loadGeofence();
  }

  @override
  void dispose() {
    _positionSub?.cancel();
    super.dispose();
  }

  Future<void> _startListeningToGps() async {
    // Kick off the GPS stream (permission request included).
    await LocationService.instance.startWatching();

    // Use the last known position immediately if available (no wait needed).
    final existing = LocationService.instance.lastKnownPosition;
    if (existing != null && mounted) {
      final latLng = LatLng(existing.latitude, existing.longitude);
      setState(() {
        _myPosition = latLng;
        _isOutOfBounds = _checkOutOfBounds(latLng);
      });
    }

    // Subscribe to live updates.
    _positionSub = LocationService.instance.positionStream.listen((pos) {
      if (!mounted) return;
      final latLng = LatLng(pos.latitude, pos.longitude);
      setState(() {
        _myPosition = latLng;
        _isOutOfBounds = _checkOutOfBounds(latLng);
      });

      if (_followMode) {
        _mapController.move(latLng, _mapController.camera.zoom);
      }
    });
  }

  /// Loads the geofence polygon for the active session from the backend.
  Future<void> _loadGeofence() async {
    try {
      final sessionId = await ApiService.instance.getActiveSessionId();
      if (sessionId == null || !mounted) return;
      final points = await ApiService.instance.loadGeofencePoints(sessionId);
      if (!mounted) return;
      setState(() {
        _geofencePoints =
            points.map((p) => LatLng(p.latitude, p.longitude)).toList();
        // Re-evaluate out-of-bounds with the freshly loaded polygon.
        if (_myPosition != null) {
          _isOutOfBounds = _checkOutOfBounds(_myPosition!);
        }
      });
    } catch (_) {
      // Network unavailable or no session – silently ignore.
    }
  }

  /// Returns true when [point] lies outside the [_geofencePoints] polygon.
  /// Uses the Ray Casting algorithm. Returns false when no polygon is defined.
  bool _checkOutOfBounds(LatLng point) {
    if (_geofencePoints.length < 3) return false;
    return !_isPointInPolygon(point, _geofencePoints);
  }

  /// Ray Casting point-in-polygon check.
  static bool _isPointInPolygon(LatLng point, List<LatLng> polygon) {
    final x = point.longitude;
    final y = point.latitude;
    bool inside = false;
    int j = polygon.length - 1;
    for (int i = 0; i < polygon.length; i++) {
      final xi = polygon[i].longitude;
      final yi = polygon[i].latitude;
      final xj = polygon[j].longitude;
      final yj = polygon[j].latitude;
      final intersect =
          ((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
      if (intersect) inside = !inside;
      j = i;
    }
    return inside;
  }

  List<Marker> _buildAllMarkers() {
    final markers = <Marker>[];

    // Real GPS position of the current player.
    if (_myPosition != null) {
      markers.add(_buildMarker(PlayerMarker(
        id: 'me',
        name: 'You',
        position: _myPosition!,
        color: Colors.blue,
        isCurrentUser: true,
      )));
    }

    // Other players (currently hardcoded – will come from API later).
    markers.addAll(_otherPlayers.map(_buildMarker));

    return markers;
  }

  @override
  Widget build(BuildContext context) {
    final center = _myPosition ?? _fallbackCenter;

    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF1E293B),
        border: Border(
          bottom: BorderSide(color: Colors.blue.shade700, width: 2),
        ),
      ),
      child: Stack(
        children: [
          FlutterMap(
            mapController: _mapController,
            options: MapOptions(
              initialCenter: center,
              initialZoom: 15.0,
              minZoom: 10.0,
              maxZoom: 18.0,
              // Disable follow mode as soon as the user manually drags the map.
              onPositionChanged: (_, hasGesture) {
                if (hasGesture && _followMode) {
                  setState(() => _followMode = false);
                }
              },
            ),
            children: [
              TileLayer(
                urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                userAgentPackageName: 'com.xact.app',
                tileBuilder: (context, tileWidget, tile) {
                  return ColorFiltered(
                    colorFilter: const ColorFilter.matrix(<double>[
                      0.2126,
                      0.7152,
                      0.0722,
                      0,
                      0,
                      0.2126,
                      0.7152,
                      0.0722,
                      0,
                      0,
                      0.2126,
                      0.7152,
                      0.0722,
                      0,
                      0,
                      0,
                      0,
                      0,
                      1,
                      0,
                    ]),
                    child: tileWidget,
                  );
                },
              ),
              MarkerLayer(markers: _buildAllMarkers()),
              // Geofence polygon (shown for all players once the host saves it)
              if (_geofencePoints.length >= 3)
                PolygonLayer(
                  polygons: [
                    Polygon(
                      points: _geofencePoints,
                      color: Colors.blue.withValues(alpha: 0.07),
                      borderColor: Colors.blue.shade400,
                      borderStrokeWidth: 2.5,
                    ),
                  ],
                ),
            ],
          ),
          const MapHeader(),
          const MapLegend(),
          // Out-of-bounds warning banner
          if (_isOutOfBounds)
            Positioned(
              top: 0,
              left: 0,
              right: 0,
              child: Material(
                color: Colors.transparent,
                child: Container(
                  padding: const EdgeInsets.symmetric(vertical: 10),
                  color: Colors.red.shade800.withValues(alpha: 0.93),
                  child: const Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.warning_amber_rounded,
                          color: Colors.white, size: 18),
                      SizedBox(width: 8),
                      Text(
                        'You are outside the game area!',
                        style: TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          // "No GPS fix yet" indicator
          if (_myPosition == null)
            Positioned(
              top: 8,
              left: 0,
              right: 0,
              child: Center(
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: Colors.black87,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: const Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      SizedBox(
                        width: 12,
                        height: 12,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.amber),
                      ),
                      SizedBox(width: 8),
                      Text('Acquiring GPS…', style: TextStyle(color: Colors.amber, fontSize: 12)),
                    ],
                  ),
                ),
              ),
            ),
          Positioned(
            bottom: 16,
            right: 16,
            child: Column(
              children: [
                _ZoomButton(
                  icon: Icons.add,
                  onPressed: () {
                    final currentZoom = _mapController.camera.zoom;
                    _mapController.move(
                      _mapController.camera.center,
                      currentZoom + 1,
                    );
                  },
                ),
                const SizedBox(height: 8),
                _ZoomButton(
                  icon: Icons.remove,
                  onPressed: () {
                    final currentZoom = _mapController.camera.zoom;
                    _mapController.move(
                      _mapController.camera.center,
                      currentZoom - 1,
                    );
                  },
                ),
                const SizedBox(height: 8),
                // my_location button: re-enable follow mode and snap to position.
                _ZoomButton(
                  icon: _followMode ? Icons.my_location : Icons.location_searching,
                  onPressed: () {
                    final pos = _myPosition;
                    if (pos == null) return;
                    setState(() => _followMode = true);
                    _mapController.move(pos, 15.0);
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Marker _buildMarker(PlayerMarker player) {
    return Marker(
      point: player.position,
      width: player.isCurrentUser || player.isMisterX ? 50 : 40,
      height: player.isCurrentUser || player.isMisterX ? 50 : 40,
      child: _PlayerMarkerWidget(player: player),
    );
  }
}

class PlayerMarker {
  final String id;
  final String name;
  final LatLng position;
  final Color color;
  final bool isCurrentUser;
  final bool isMisterX;

  PlayerMarker({
    required this.id,
    required this.name,
    required this.position,
    required this.color,
    this.isCurrentUser = false,
    this.isMisterX = false,
  });
}

class _PlayerMarkerWidget extends StatelessWidget {
  final PlayerMarker player;

  const _PlayerMarkerWidget({required this.player});

  @override
  Widget build(BuildContext context) {
    final size = player.isCurrentUser || player.isMisterX ? 50.0 : 40.0;
    final iconSize = player.isCurrentUser || player.isMisterX ? 28.0 : 22.0;

    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: player.color,
        shape: BoxShape.circle,
        border: Border.all(color: Colors.white, width: 3),
        boxShadow: [
          BoxShadow(
            color: player.color.withValues(alpha: 0.5),
            blurRadius: 8,
            spreadRadius: 2,
          ),
        ],
      ),
      child: Icon(
        player.isMisterX
            ? Icons.help_outline
            : (player.isCurrentUser ? Icons.person : Icons.person_outline),
        color: Colors.white,
        size: iconSize,
      ),
    );
  }
}

class _ZoomButton extends StatelessWidget {
  final IconData icon;
  final VoidCallback onPressed;

  const _ZoomButton({required this.icon, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF0F172A).withValues(alpha: 0.9),
        borderRadius: BorderRadius.circular(8),
      ),
      child: IconButton(
        icon: Icon(icon, color: Colors.white),
        onPressed: onPressed,
        splashRadius: 24,
      ),
    );
  }
}
