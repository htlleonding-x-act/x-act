import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../xact_branding.dart';

class DefineGameAreaMap extends StatelessWidget {
  final MapController mapController;
  final LatLng fallbackCenter;
  final LatLng? myLocation;
  final List<LatLng> points;
  final int selectedIndex;
  final bool isMoveMode;
  final TapCallback onMapTap;
  final ValueChanged<TapDownDetails> Function(int index) onPointTapDown;

  const DefineGameAreaMap({
    super.key,
    required this.mapController,
    required this.fallbackCenter,
    required this.myLocation,
    required this.points,
    required this.selectedIndex,
    required this.isMoveMode,
    required this.onMapTap,
    required this.onPointTapDown,
  });

  @override
  Widget build(BuildContext context) {
    return FlutterMap(
      mapController: mapController,
      options: MapOptions(
        initialCenter: fallbackCenter,
        initialZoom: 15.0,
        minZoom: 10.0,
        maxZoom: 18.0,
        onTap: onMapTap,
        interactionOptions: const InteractionOptions(
          flags: InteractiveFlag.all,
        ),
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
        if (points.length >= 3)
          PolygonLayer(
            polygons: [
              Polygon(
                points: points,
                color: XActColors.secondary.withValues(alpha: 0.16),
                borderColor: XActColors.secondary,
                borderStrokeWidth: 2.5,
              ),
            ],
          ),
        if (points.length >= 2)
          PolylineLayer(
            polylines: [
              Polyline(
                points: points,
                color: XActColors.secondary,
                strokeWidth: 2.5,
                pattern: StrokePattern.dashed(segments: const [8, 6]),
              ),
            ],
          ),
        if (myLocation != null)
          MarkerLayer(
            markers: [
              Marker(
                point: myLocation!,
                width: 40,
                height: 40,
                child: Center(
                  child: Container(
                    width: 18,
                    height: 18,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: XActColors.primary,
                      border: Border.all(color: Colors.white, width: 3),
                      boxShadow: [
                        BoxShadow(
                          color: XActColors.primary.withValues(alpha: .6),
                          blurRadius: 12,
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        MarkerLayer(
          markers: [
            for (var i = 0; i < points.length; i++)
              Marker(
                point: points[i],
                width: 44,
                height: 44,
                child: GestureDetector(
                  onTapDown: onPointTapDown(i),
                  onSecondaryTapDown: onPointTapDown(i),
                  child: DefineGameAreaPointMarker(
                    index: i + 1,
                    isSelected: selectedIndex == i,
                    isMoveMode: isMoveMode && selectedIndex == i,
                  ),
                ),
              ),
          ],
        ),
      ],
    );
  }
}

class DefineGameAreaPointMarker extends StatelessWidget {
  final int index;
  final bool isSelected;
  final bool isMoveMode;

  const DefineGameAreaPointMarker({
    super.key,
    required this.index,
    this.isSelected = false,
    this.isMoveMode = false,
  });

  @override
  Widget build(BuildContext context) {
    final size = isMoveMode ? 44.0 : 34.0;
    final color = isMoveMode
        ? XActColors.warning
        : isSelected
            ? XActColors.success
            : XActColors.secondary;

    return Center(
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: color,
          shape: BoxShape.circle,
          border: Border.all(
            color: Colors.white,
            width: isMoveMode || isSelected ? 3 : 2,
          ),
          boxShadow: [
            BoxShadow(
              color: color.withValues(
                alpha: isMoveMode ? 0.9 : isSelected ? 0.7 : 0.5,
              ),
              blurRadius: isMoveMode ? 14 : isSelected ? 10 : 6,
              spreadRadius: isMoveMode ? 3 : isSelected ? 2 : 1,
            ),
          ],
        ),
        child: Center(
          child: Text(
            '$index',
            style: XActText.bodySm.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w700,
              fontSize: isMoveMode ? 15 : 13,
            ),
          ),
        ),
      ),
    );
  }
}

class DefineGameAreaCrosshairOverlay extends StatelessWidget {
  const DefineGameAreaCrosshairOverlay({super.key});

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: SizedBox(
        width: 24,
        height: 24,
        child: CustomPaint(painter: _CrosshairPainter()),
      ),
    );
  }
}

class _CrosshairPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withValues(alpha: 0.5)
      ..strokeWidth = 1.0;
    final cx = size.width / 2;
    final cy = size.height / 2;
    canvas.drawLine(Offset(cx - 10, cy), Offset(cx + 10, cy), paint);
    canvas.drawLine(Offset(cx, cy - 10), Offset(cx, cy + 10), paint);
  }

  @override
  bool shouldRepaint(_) => false;
}

class DefineGameAreaBottomPanel extends StatelessWidget {
  final String statusText;
  final int pointCount;
  final bool isSaving;
  final bool canSave;
  final VoidCallback onSave;

  const DefineGameAreaBottomPanel({
    super.key,
    required this.statusText,
    required this.pointCount,
    required this.isSaving,
    required this.canSave,
    required this.onSave,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      decoration: BoxDecoration(
        color: XActColors.bg.withValues(alpha: .95),
        border: Border(top: BorderSide(color: XActColors.hairlineSoft)),
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    statusText,
                    style: XActText.bodySm.copyWith(
                      color: XActColors.text2,
                      fontSize: 13,
                    ),
                  ),
                  if (pointCount > 0) ...[
                    const SizedBox(height: 4),
                    Text(
                      '$pointCount ${pointCount == 1 ? 'corner' : 'corners'} placed',
                      style: XActText.caption.copyWith(
                        color: XActColors.secondary,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(width: 14),
            SizedBox(
              width: 150,
              child: XActBranding.buildSuccessButton(
                text: isSaving ? 'Saving…' : 'Save area',
                icon: Icons.check_rounded,
                height: 48,
                onPressed: (canSave && !isSaving) ? onSave : null,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
