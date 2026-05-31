import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class MapPreviewScreen extends StatefulWidget {
  final int sessionId;
  final String gameName;

  const MapPreviewScreen({
    super.key,
    required this.sessionId,
    required this.gameName,
  });

  @override
  State<MapPreviewScreen> createState() => _MapPreviewScreenState();
}

class _MapPreviewScreenState extends State<MapPreviewScreen> {
  bool _loading = true;
  List<LatLng> _points = const [];

  @override
  void initState() {
    super.initState();
    _loadGeofence();
  }

  Future<void> _loadGeofence() async {
    try {
      final pts =
          await ApiService.instance.loadGeofencePoints(widget.sessionId) ??
          const [];
      if (!mounted) return;
      setState(() {
        _points = pts
            .map((p) => LatLng(p.latitude, p.longitude))
            .toList(growable: false);
      });
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  LatLng get _mapCenter {
    if (_points.isEmpty) return const LatLng(48.2082, 16.3738);
    final avgLat =
        _points.map((p) => p.latitude).reduce((a, b) => a + b) / _points.length;
    final avgLng =
        _points.map((p) => p.longitude).reduce((a, b) => a + b) /
        _points.length;
    return LatLng(avgLat, avgLng);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      appBar: AppBar(
        backgroundColor: XActColors.bg,
        foregroundColor: XActColors.text1,
        elevation: 0,
        scrolledUnderElevation: 0,
        titleSpacing: 0,
        title: Padding(
          padding: const EdgeInsets.only(left: 4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              XActBranding.buildEyebrow('Map Preview'),
              Text(
                widget.gameName,
                style: XActText.heading,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(color: XActColors.secondary),
            )
          : _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_points.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.map_outlined, size: 64, color: XActColors.text4),
            const SizedBox(height: 16),
            Text(
              'No map area defined yet',
              style: XActText.body.copyWith(color: XActColors.text3),
            ),
            const SizedBox(height: 4),
            Text(
              'The host hasn\'t set a play area.',
              style: XActText.caption.copyWith(color: XActColors.text4),
            ),
          ],
        ),
      );
    }

    return FlutterMap(
      options: MapOptions(
        initialCenter: _mapCenter,
        initialZoom: 15.0,
        minZoom: 10.0,
        maxZoom: 18.0,
        interactionOptions: const InteractionOptions(
          flags: InteractiveFlag.all,
        ),
      ),
      children: [
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.xact.app',
          tileBuilder: (context, tileWidget, tile) => ColorFiltered(
            colorFilter: const ColorFilter.matrix(<double>[
              0.2126, 0.7152, 0.0722, 0, 0,
              0.2126, 0.7152, 0.0722, 0, 0,
              0.2126, 0.7152, 0.0722, 0, 0,
              0,      0,      0,      1, 0,
            ]),
            child: tileWidget,
          ),
        ),
        PolygonLayer(
          polygons: [
            Polygon(
              points: _points,
              color: XActColors.secondary.withValues(alpha: 0.16),
              borderColor: XActColors.secondary,
              borderStrokeWidth: 2.0,
            ),
          ],
        ),
      ],
    );
  }
}
