import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:geolocator/geolocator.dart';
import 'package:latlong2/latlong.dart';
import '../api/api_service.dart';
import '../api/models.dart';
import '../constants.dart';
import '../services/app_session.dart';
import '../services/geofence_store.dart';
import '../services/location_service.dart';
import 'map_header.dart';
import 'map_legend.dart';
import 'xact_branding.dart';

class MapArea extends StatefulWidget {
  final VoidCallback? onFullscreenToggle;
  final bool isFullscreen;

  const MapArea({
    super.key,
    this.onFullscreenToggle,
    this.isFullscreen = false,
  });

  @override
  State<MapArea> createState() => _MapAreaState();
}

class _MapAreaState extends State<MapArea> {
  final MapController _mapController = MapController();

  static const LatLng _fallbackCenter = kFallbackMapCenter;

  LatLng? _myPosition;

  // follows the player's gps position. it starts off when there is an area to
  // show, so the area stays framed until the user recenters on themselves
  late bool _followMode;

  late LatLng _initialMapCenter;

  bool _showControls = false;

  StreamSubscription<Position>? _positionSub;
  StreamSubscription<GameSessionSnapshot>? _realtimeSnapshotSub;

  List<LatLng> _geofencePoints = [];
  bool _isOutOfBounds = false;

  List<PlayerMarker> _otherPlayers = [];
  List<MapLegendTeamEntry> _legendTeamEntries = const [];
  Color _myMarkerColor = XActColors.secondary;

  @override
  void initState() {
    super.initState();

    // start from the cached geofence, saved by the host or a previous load, so
    // the very first frame already shows the game area
    final cachedGeofence = GeofenceStore.instance.points;
    if (cachedGeofence.length >= 3) {
      _geofencePoints = List.of(cachedGeofence);
      _initialMapCenter = LatLngBounds.fromPoints(_geofencePoints).center;
      _followMode = false;
    } else {
      final lastFix = LocationService.instance.lastKnownPosition;
      _initialMapCenter = lastFix != null
          ? LatLng(lastFix.latitude, lastFix.longitude)
          : _fallbackCenter;
      _followMode = true;
    }

    _startListeningToGps();
    _loadSessionGeofence();
    _refreshPlayers();
    _listenToRealtimeUpdates();

    // fit the camera on the next frame, initialCenter alone doesn't pick the
    // right zoom for the polygon
    if (_geofencePoints.length >= 3) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _fitCameraToGeofence();
      });
    }
  }

  void _fitCameraToGeofence() {
    if (!mounted || _geofencePoints.length < 3) return;
    try {
      _mapController.fitCamera(
        CameraFit.bounds(
          bounds: LatLngBounds.fromPoints(_geofencePoints),
          padding: const EdgeInsets.all(40),
          maxZoom: 17.0,
        ),
      );
    } catch (_) {
      // the map controller isn't attached yet, the next load tries again
    }
  }

  @override
  void dispose() {
    _positionSub?.cancel();
    _realtimeSnapshotSub?.cancel();
    super.dispose();
  }

  Future<void> _listenToRealtimeUpdates() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      return;
    }

    try {
      await ApiService.instance.ensureRealtimeSessionSubscription(sessionId);

      // RealtimeService patches these snapshots locally on each event because a
      // server snapshot reads the whole location history and costs too much per
      // ping
      _realtimeSnapshotSub =ApiService.instance.realtimeSnapshots.listen((
        snapshot,
      ) {
        if (snapshot.sessionId == sessionId) {
          _refreshPlayers(snapshot);
        }
      });
    } catch (_) {
      // the map keeps working through the regular http fallback
    }
  }

  Future<void> _loadSessionGeofence() async {
    final sessionId = AppSession.instance.currentSessionId;
    if (sessionId == null) {
      final cached = GeofenceStore.instance.points;
      _geofencePoints = cached.length >= 3 ? List.of(cached) : [];
      return;
    }

    final hadGeofenceBefore = _geofencePoints.length >= 3;

    try {
      final points = await ApiService.instance.loadGeofencePoints(sessionId);
      if (!mounted) return;
      final loaded = points
              ?.map((p) => LatLng(p.latitude, p.longitude))
              .toList(growable: false) ??
          const <LatLng>[];
      setState(() {
        _geofencePoints = loaded;
      });
      if (loaded.length >= 3) {
        // cache it so the next MapArea, e.g. after toggling fullscreen, starts
        // framed on the area without asking the backend
        GeofenceStore.instance.setPoints(loaded);
        // only auto fit when this load brought in the polygon. a refresh in
        // place shouldn't pull the camera away from the user
        if (!hadGeofenceBefore) {
          setState(() => _followMode = false);
          WidgetsBinding.instance.addPostFrameCallback((_) {
            _fitCameraToGeofence();
          });
        }
      }
    } catch (_) {
      if (!mounted) return;
      final cached = GeofenceStore.instance.points;
      setState(() {
        _geofencePoints = cached.length >= 3 ? List.of(cached) : [];
      });
    }
  }

  Future<void> _refreshPlayers([GameSessionSnapshot? realtimeSnapshot]) async {
    final sessionId = AppSession.instance.currentSessionId;
    final myMemberId = AppSession.instance.currentMemberId;
    if (sessionId == null) return;

    try {
      final snapshot = realtimeSnapshot != null
          ? await ApiService.instance.toLobbySnapshot(realtimeSnapshot)
          : await ApiService.instance.loadLobbySnapshot(sessionId);
      final players = await ApiService.instance.loadPlayerPositions(
        sessionId,
        from: snapshot,
      );
      final legendTeams = await ApiService.instance.loadMapLegendTeams(
        sessionId,
        from: snapshot,
      );

      if (!mounted) return;

      setState(() {
        final currentTeamId = AppSession.instance.currentTeamId;
        final currentTeamColor = currentTeamId == null
            ? null
            : legendTeams
                  .where((team) => team.teamId == currentTeamId)
                  .map((team) => team.color)
                  .firstOrNull;

        _otherPlayers = players
            .where((p) => p.memberId != myMemberId)
            .map(
              (p) => PlayerMarker(
                id: p.memberId.toString(),
                name: p.displayName,
                position: p.position,
                color: p.color,
                isMisterX: p.teamRole == TeamRole.mrX,
              ),
            )
            .toList(growable: false);

        _legendTeamEntries = legendTeams
            .map(
              (team) => MapLegendTeamEntry(
                label: team.label,
                color: team.color,
              ),
            )
            .toList(growable: false);

        _myMarkerColor = currentTeamColor ?? XActColors.secondary;
      });
    } catch (_) {
      // keep the current markers when a refresh fails
    }
  }

  Future<void> _startListeningToGps() async {
    // this also asks for the location permission
    await LocationService.instance.startWatching();

    final existing = LocationService.instance.lastKnownPosition;
    if (existing != null) {
      _applyPosition(existing);
    }

    _positionSub = LocationService.instance.positionStream.listen(_applyPosition);

  }

  void _applyPosition(Position pos) {
    if (!mounted) return;
    final latLng = LatLng(pos.latitude, pos.longitude);
    setState(() {
      _myPosition = latLng;
      _isOutOfBounds = _checkOutOfBounds(latLng);
    });

    if (_followMode) {
      _mapController.move(latLng, _mapController.camera.zoom);
    }
  }

  bool _checkOutOfBounds(LatLng point) {
    if (_geofencePoints.length < 3) return false;
    return !_isPointInPolygon(point, _geofencePoints);
  }

  /// ray casting point in polygon test
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

    if (_myPosition != null) {
      markers.add(
        _buildMarker(
          PlayerMarker(
            id: 'me',
            name: 'You',
            position: _myPosition!,
            color: _myMarkerColor,
            isCurrentUser: true,
          ),
        ),
      );
    }

    markers.addAll(_otherPlayers.map(_buildMarker));

    return markers;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: XActColors.bg2,
        border: Border(
          bottom: BorderSide(color: XActColors.hairlineSoft),
        ),
      ),
      child: Stack(
        children: [
          FlutterMap(
            mapController: _mapController,
            options: MapOptions(
              initialCenter: _initialMapCenter,
              initialZoom: 15.0,
              minZoom: 10.0,
              maxZoom: 18.0,
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
              if (_geofencePoints.length >= 3)
                PolygonLayer(
                  polygons: [
                    Polygon(
                      points: _geofencePoints,
                      color: XActColors.secondary.withValues(alpha: 0.10),
                      borderColor: XActColors.secondary,
                      borderStrokeWidth: 2.5,
                    ),
                  ],
                ),
            ],
          ),
          const MapHeader(),
          MapLegend(
            teamEntries: _legendTeamEntries,
            myLocationColor: _myMarkerColor,
          ),
          if (_isOutOfBounds)
            Positioned(
              top: 100,
              left: 16,
              right: 16,
              child: Material(
                color: Colors.transparent,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 12,
                  ),
                  decoration: BoxDecoration(
                    color: XActColors.primary.withValues(alpha: .92),
                    borderRadius: BorderRadius.circular(14),
                    boxShadow: XActElevation.glowRed,
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(
                        Icons.warning_amber_rounded,
                        color: Colors.white,
                        size: 18,
                      ),
                      const SizedBox(width: 8),
                      Text(
                        'You are outside the game area',
                        style: XActText.bodySm.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          if (_myPosition == null)
            Positioned(
              top: 100,
              left: 0,
              right: 0,
              child: Center(
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 8,
                  ),
                  decoration: BoxDecoration(
                    color: XActColors.glass,
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: XActColors.hairlineSoft),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const SizedBox(
                        width: 12,
                        height: 12,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: XActColors.warning,
                        ),
                      ),
                      const SizedBox(width: 10),
                      Text(
                        'Acquiring GPS…',
                        style: XActText.caption.copyWith(
                          color: XActColors.warning,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          Positioned(
            bottom: 16,
            right: 16,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_showControls) ...[
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
                  _ZoomButton(
                    icon: _followMode
                        ? Icons.my_location
                        : Icons.location_searching,
                    onPressed: () {
                      final pos = _myPosition;
                      if (pos == null) return;
                      setState(() => _followMode = true);
                      _mapController.move(pos, 15.0);
                    },
                  ),
                  if (widget.onFullscreenToggle != null) ...[
                    const SizedBox(height: 8),
                    _ZoomButton(
                      icon: widget.isFullscreen
                          ? Icons.fullscreen_exit
                          : Icons.fullscreen,
                      onPressed: widget.onFullscreenToggle!,
                    ),
                  ],
                  const SizedBox(height: 8),
                ],
                _ZoomButton(
                  icon: _showControls ? Icons.close : Icons.menu,
                  onPressed: () =>
                      setState(() => _showControls = !_showControls),
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
            ? Icons.location_on
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
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: XActColors.glass,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e2,
      ),
      child: IconButton(
        icon: Icon(icon, color: XActColors.text1, size: 20),
        onPressed: onPressed,
        splashRadius: 22,
      ),
    );
  }
}
