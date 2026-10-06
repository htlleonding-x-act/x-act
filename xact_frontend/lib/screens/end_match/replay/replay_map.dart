import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../../api/game_results.dart';
import '../../../constants.dart';
import '../../../widgets/map_tiles.dart';
import '../../../widgets/xact_branding.dart';
import 'replay_controller.dart';
import 'replay_markers.dart';
import 'replay_tracks.dart';

class ReplayMap extends StatefulWidget {
  const ReplayMap({
    super.key,
    required this.results,
    required this.tracks,
    required this.controller,
    required this.mapController,
    required this.highlightedEvent,
    required this.onMapReady,
  });

  final GameResults results;
  final ReplayTrackSet tracks;
  final ReplayController controller;
  final MapController mapController;
  final ValueNotifier<TimelineEvent?> highlightedEvent;
  final VoidCallback onMapReady;

  @override
  State<ReplayMap> createState() => _ReplayMapState();
}

class _ReplayMapState extends State<ReplayMap> {
  static const double _playerSize = 30;
  static const double _focusedPlayerSize = 38;
  static const double _fitMaxZoom = 17;

  // the map first lays out while the header above still shrinks, so it fits
  // the routes again on every resize until someone moves the camera
  bool _cameraMoved = false;
  bool _mapReady = false;
  bool _refitting = false;
  Size? _fittedSize;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final size = constraints.biggest;
        final fitPadding = _fitPadding(size.height);
        _fittedSize ??= size;
        if (size != _fittedSize) {
          _refitAfterLayout(size, fitPadding);
        }
        return _buildMap(fitPadding);
      },
    );
  }

  /// flutter_map applies the initial fit in the same frame, right after it
  /// reports ready. waiting for that keeps a jump from the ready callback from
  /// being overwritten, and the fit itself doesn't count as a camera move
  void _onMapReady() {
    scheduleMicrotask(() {
      if (!mounted) return;
      _mapReady = true;
      _cameraMoved = false;
      widget.onMapReady();
    });
  }

  void _onMapEvent(MapEvent event) {
    if (event is MapEventWithMove &&
        !_refitting &&
        event.source != MapEventSource.nonRotatedSizeChange) {
      _cameraMoved = true;
    }
  }

  void _refitAfterLayout(Size size, EdgeInsets fitPadding) {
    final bounds = widget.tracks.bounds;
    if (bounds == null) return;

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted || !_mapReady || _cameraMoved) return;
      _fittedSize = size;
      // a fit reports itself as a controller move, which would end the refits
      _refitting = true;
      widget.mapController.fitCamera(
        CameraFit.bounds(
          bounds: bounds,
          padding: fitPadding,
          maxZoom: _fitMaxZoom,
        ),
      );
      _refitting = false;
    });
  }

  /// keeps the routes clear of the chips on top and the controls at the
  /// bottom. on a short phone map fixed paddings would eat most of the height
  static EdgeInsets _fitPadding(double mapHeight) => EdgeInsets.fromLTRB(
    24,
    min(72, mapHeight * .2),
    24,
    min(88, mapHeight * .25),
  );

  Widget _buildMap(EdgeInsets fitPadding) {
    final bounds = widget.tracks.bounds;

    return FlutterMap(
      mapController: widget.mapController,
      options: MapOptions(
        initialCenter: kFallbackMapCenter,
        initialZoom: 15,
        initialCameraFit: bounds == null
            ? null
            : CameraFit.bounds(
                bounds: bounds,
                padding: fitPadding,
                maxZoom: _fitMaxZoom,
              ),
        minZoom: 10,
        maxZoom: 19,
        backgroundColor: XActColors.bg2,
        keepAlive: true,
        onMapEvent: _onMapEvent,
        onMapReady: _onMapReady,
      ),
      children: [
        buildGreyscaleTileLayer(),
        if (widget.results.geofence.length >= 3)
          PolygonLayer(
            polygons: [
              Polygon(
                points: widget.results.geofence,
                color: XActColors.secondary.withValues(alpha: .06),
                borderColor: XActColors.secondary.withValues(alpha: .6),
                borderStrokeWidth: 2,
              ),
            ],
          ),
        // the whole routes never change, so they skip the per frame rebuild
        RepaintBoundary(
          child: PolylineLayer(
            polylines: [for (final track in widget.tracks.tracks) ...track.fullRoute()],
          ),
        ),
        ListenableBuilder(
          listenable: Listenable.merge([widget.controller, widget.highlightedEvent]),
          builder: (context, _) =>
              Stack(fit: StackFit.expand, children: _buildDynamicLayers()),
        ),
      ],
    );
  }

  List<Widget> _buildDynamicLayers() {
    final seconds = widget.controller.positionSeconds;
    final visible = widget.tracks.tracks
        .where((t) => !widget.controller.hiddenMemberIds.contains(t.member.memberId))
        .toList();

    final trails = <Polyline>[];
    final heads = <Polyline>[];
    final players = <Marker>[];
    for (final track in visible) {
      final index = track.indexAt(seconds);
      trails.addAll(track.trailUpTo(index));
      final position = track.positionAt(seconds);
      if (position == null) {
        continue;
      }

      final isMrX = track.isMrXAt(seconds);
      heads.add(
        Polyline(
          points: [track.pointAt(index), position],
          color: isMrX ? XActColors.roleMrX : track.color,
          strokeWidth: isMrX ? 4 : 3,
        ),
      );

      final focused = widget.controller.focusedMemberId == track.member.memberId;
      final size = focused ? _focusedPlayerSize : _playerSize;
      players.add(
        Marker(
          point: position,
          width: size,
          height: size,
          child: ReplayPlayerMarker(
            name: track.member.displayName,
            color: track.color,
            isMrX: isMrX,
            focused: focused,
          ),
        ),
      );
    }

    final highlight = widget.highlightedEvent.value;
    final pins = <Marker>[
      for (final pin in widget.tracks.revealPins)
        if (pin.offsetSeconds <= seconds)
          Marker(
            point: pin.position,
            width: 18,
            height: 18,
            child: const RevealPinMarker(),
          ),
      for (final event in widget.results.catches)
        if (event.position != null && event.offsetSeconds <= seconds)
          Marker(
            point: event.position!,
            width: 26,
            height: 26,
            child: const CatchMarker(),
          ),
      if (highlight?.position case final LatLng position)
        Marker(
          point: position,
          width: 64,
          height: 64,
          child: const PulseHighlight(color: XActColors.warning),
        ),
    ];

    return [
      PolylineLayer(polylines: trails),
      PolylineLayer(polylines: heads),
      MarkerLayer(markers: pins),
      // players are drawn last so a pin never hides someone
      MarkerLayer(markers: players),
    ];
  }
}
