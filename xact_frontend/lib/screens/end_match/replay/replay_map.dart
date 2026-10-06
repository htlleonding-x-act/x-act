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

class ReplayMap extends StatelessWidget {
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

  static const double _playerSize = 30;
  static const double _focusedPlayerSize = 38;

  @override
  Widget build(BuildContext context) {
    final bounds = tracks.bounds;

    return FlutterMap(
      mapController: mapController,
      options: MapOptions(
        initialCenter: kFallbackMapCenter,
        initialZoom: 15,
        initialCameraFit: bounds == null
            ? null
            : CameraFit.bounds(
                bounds: bounds,
                padding: const EdgeInsets.fromLTRB(32, 72, 32, 88),
                maxZoom: 17,
              ),
        minZoom: 10,
        maxZoom: 19,
        backgroundColor: XActColors.bg2,
        keepAlive: true,
        onMapReady: onMapReady,
      ),
      children: [
        buildGreyscaleTileLayer(),
        if (results.geofence.length >= 3)
          PolygonLayer(
            polygons: [
              Polygon(
                points: results.geofence,
                color: XActColors.secondary.withValues(alpha: .06),
                borderColor: XActColors.secondary.withValues(alpha: .6),
                borderStrokeWidth: 2,
              ),
            ],
          ),
        // the whole routes never change, so they skip the per frame rebuild
        RepaintBoundary(
          child: PolylineLayer(
            polylines: [for (final track in tracks.tracks) ...track.fullRoute()],
          ),
        ),
        ListenableBuilder(
          listenable: Listenable.merge([controller, highlightedEvent]),
          builder: (context, _) =>
              Stack(fit: StackFit.expand, children: _buildDynamicLayers()),
        ),
      ],
    );
  }

  List<Widget> _buildDynamicLayers() {
    final seconds = controller.positionSeconds;
    final visible = tracks.tracks
        .where((t) => !controller.hiddenMemberIds.contains(t.member.memberId))
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

      final focused = controller.focusedMemberId == track.member.memberId;
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

    final highlight = highlightedEvent.value;
    final pins = <Marker>[
      for (final pin in tracks.revealPins)
        if (pin.offsetSeconds <= seconds)
          Marker(
            point: pin.position,
            width: 18,
            height: 18,
            child: const RevealPinMarker(),
          ),
      for (final event in results.catches)
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
