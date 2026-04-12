import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../services/geofence_store.dart';
import '../../services/location_service.dart';

class DefineGameAreaScreen extends StatefulWidget {
  final int sessionId;

  const DefineGameAreaScreen({super.key, required this.sessionId});

  @override
  State<DefineGameAreaScreen> createState() => _DefineGameAreaScreenState();
}

class _DefineGameAreaScreenState extends State<DefineGameAreaScreen> {
  final MapController _mapController = MapController();

  // Points the host has placed – these form the polygon.
  final List<LatLng> _points = [];

  // Index of the currently selected point, -1 when none is selected.
  int _selectedIndex = -1;

  // When true, the next map tap repositions the selected point.
  bool _isMoveMode = false;

  static const LatLng _fallbackCenter = LatLng(48.3069, 14.2858);

  @override
  void initState() {
    super.initState();
    _centerOnPlayer();
  }

  /// Centers the map on the player's GPS position if available.
  void _centerOnPlayer() {
    final pos = LocationService.instance.lastKnownPosition;
    if (pos != null) {
      // Schedule after the first frame so the MapController is ready.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          _mapController.move(LatLng(pos.latitude, pos.longitude), 15.0);
        }
      });
    }
  }

  void _onMapTap(TapPosition _, LatLng point) {
    if (_isMoveMode && _selectedIndex >= 0 && _selectedIndex < _points.length) {
      setState(() {
        _points[_selectedIndex] = point;
        _isMoveMode = false;
      });
      return;
    }

    setState(() {
      _points.add(point);
      _selectedIndex = -1;
      _isMoveMode = false;
    });
  }

  void _deletePointAt(int index) {
    if (index < 0 || index >= _points.length) return;
    setState(() {
      _points.removeAt(index);

      if (_selectedIndex == index) {
        _selectedIndex = -1;
        _isMoveMode = false;
      } else if (_selectedIndex > index) {
        _selectedIndex -= 1;
      }
    });
  }

  Future<void> _showPointMenu(int index, Offset globalPosition) async {
    if (index < 0 || index >= _points.length) return;

    setState(() {
      _selectedIndex = index;
    });

    final overlay = Overlay.of(context).context.findRenderObject() as RenderBox;
    final selection = await showMenu<_PointAction>(
      context: context,
      color: const Color(0xFF252A3A),
      position: RelativeRect.fromRect(
        Rect.fromCenter(center: globalPosition, width: 1, height: 1),
        Offset.zero & overlay.size,
      ),
      items: const [
        PopupMenuItem<_PointAction>(
          value: _PointAction.move,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.open_with, color: Colors.white),
            title: Text('Move point', style: TextStyle(color: Colors.white)),
          ),
        ),
        PopupMenuItem<_PointAction>(
          value: _PointAction.delete,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.delete_outline, color: Colors.redAccent),
            title: Text('Delete point', style: TextStyle(color: Colors.white)),
          ),
        ),
        PopupMenuItem<_PointAction>(
          value: _PointAction.deselect,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.close, color: Colors.white70),
            title: Text('Deselect', style: TextStyle(color: Colors.white70)),
          ),
        ),
      ],
    );

    if (!mounted) return;

    switch (selection) {
      case _PointAction.move:
        setState(() => _isMoveMode = true);
      case _PointAction.delete:
        _showDeleteDialog(index);
      case _PointAction.deselect:
        setState(() {
          _selectedIndex = -1;
          _isMoveMode = false;
        });
      case null:
        break;
    }
  }

  void _showDeleteDialog(int index) {
    showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF252A3A),
        title: Text(
          'Delete point ${index + 1}?',
          style: const TextStyle(color: Colors.white),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'Cancel',
              style: TextStyle(color: Colors.white54),
            ),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Delete', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    ).then((confirmed) {
      if (confirmed == true && mounted) {
        _deletePointAt(index);
      }
    });
  }

  void _undoLast() {
    if (_points.isEmpty) return;
    _deletePointAt(_points.length - 1);
  }

  void _clearAll() {
    if (_points.isEmpty) return;
    showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF252A3A),
        title: const Text('Clear area?', style: TextStyle(color: Colors.white)),
        content: const Text(
          'This will remove all placed points.',
          style: TextStyle(color: Colors.white70),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'Cancel',
              style: TextStyle(color: Colors.white54),
            ),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Clear', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    ).then((confirmed) {
      if (confirmed == true) {
        setState(() {
          _points.clear();
          _selectedIndex = -1;
          _isMoveMode = false;
        });
      }
    });
  }

  void _save() {
    if (_points.length < 3) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Place at least 3 points to define the game area.'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    GeofenceStore.instance.setPoints(List.of(_points));
    Navigator.of(context).pop(true); // signal success to caller
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  String get _statusText {
    if (_points.isEmpty) return 'Tap on the map to place the first corner';
    if (_isMoveMode && _selectedIndex >= 0) {
      return 'Move mode: tap on map to set point ${_selectedIndex + 1}';
    }
    if (_selectedIndex >= 0) {
      return 'Point ${_selectedIndex + 1} selected · choose action from menu';
    }
    if (_points.length == 1) return '1 point placed – add at least 2 more';
    if (_points.length == 2) return '2 points placed – add at least 1 more';
    return '${_points.length} points – tap point to select · tap map to add';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1A1F2E),
      appBar: AppBar(
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        title: const Text('Define Game Area'),
        actions: [
          if (_points.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.undo),
              tooltip: 'Undo last point',
              onPressed: _undoLast,
            ),
          if (_points.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.delete_sweep_outlined),
              tooltip: 'Clear all',
              onPressed: _clearAll,
            ),
        ],
      ),
      body: Stack(
        children: [
          // ── Map ────────────────────────────────────────────────
          FlutterMap(
            mapController: _mapController,
            options: MapOptions(
              initialCenter: _fallbackCenter,
              initialZoom: 15.0,
              minZoom: 10.0,
              maxZoom: 18.0,
              onTap: _onMapTap,
              interactionOptions: const InteractionOptions(
                flags: InteractiveFlag.all,
              ),
            ),
            children: [
              // Dark tile layer (same style as game map)
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
              // Filled polygon (drawn when ≥ 3 points)
              if (_points.length >= 3)
                PolygonLayer(
                  polygons: [
                    Polygon(
                      points: _points,
                      color: Colors.blue.withValues(alpha: 0.2),
                      borderColor: Colors.blue.shade400,
                      borderStrokeWidth: 2.5,
                    ),
                  ],
                ),
              // Dotted outline for < 3 points (just a polyline)
              if (_points.length >= 2)
                PolylineLayer(
                  polylines: [
                    Polyline(
                      points: _points,
                      color: Colors.blue.shade400,
                      strokeWidth: 2.5,
                      pattern: StrokePattern.dashed(segments: [8, 6]),
                    ),
                  ],
                ),
              // Numbered markers for each point
              MarkerLayer(
                markers: [
                  for (var i = 0; i < _points.length; i++)
                    Marker(
                      point: _points[i],
                      width: 44,
                      height: 44,
                      child: GestureDetector(
                        onTapDown: (details) =>
                            _showPointMenu(i, details.globalPosition),
                        onSecondaryTapDown: (details) =>
                            _showPointMenu(i, details.globalPosition),
                        child: _PointMarker(
                          index: i + 1,
                          isSelected: _selectedIndex == i,
                          isMoveMode: _isMoveMode && _selectedIndex == i,
                        ),
                      ),
                    ),
                ],
              ),
            ],
          ),

          // ── Crosshair hint overlay ──────────────────────────────
          const Center(child: _CrosshairOverlay()),

          // ── Status bar at bottom ────────────────────────────────
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: _BottomPanel(
              statusText: _statusText,
              pointCount: _points.length,
              isSaving: false,
              canSave: _points.length >= 3,
              onSave: _save,
            ),
          ),
        ],
      ),
    );
  }
}

class _PointMarker extends StatelessWidget {
  final int index;
  final bool isSelected;
  final bool isMoveMode;

  const _PointMarker({
    required this.index,
    this.isSelected = false,
    this.isMoveMode = false,
  });

  @override
  Widget build(BuildContext context) {
    final size = isMoveMode ? 44.0 : 34.0;
    final color = isMoveMode
        ? Colors.orange.shade600
        : isSelected
        ? Colors.teal.shade600
        : Colors.blue.shade700;
    return Center(
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          color: color,
          shape: BoxShape.circle,
          border: Border.all(
            color: isMoveMode
                ? Colors.white
                : isSelected
                ? Colors.tealAccent
                : Colors.white70,
            width: isMoveMode
                ? 3
                : isSelected
                ? 3
                : 2,
          ),
          boxShadow: [
            BoxShadow(
              color: color.withValues(
                alpha: isMoveMode
                    ? 0.9
                    : isSelected
                    ? 0.8
                    : 0.6,
              ),
              blurRadius: isMoveMode
                  ? 14
                  : isSelected
                  ? 10
                  : 6,
              spreadRadius: isMoveMode
                  ? 3
                  : isSelected
                  ? 2
                  : 1,
            ),
          ],
        ),
        child: Center(
          child: Text(
            '$index',
            style: TextStyle(
              color: Colors.white,
              fontSize: isMoveMode ? 15 : 13,
              fontWeight: FontWeight.bold,
            ),
          ),
        ),
      ),
    );
  }
}

class _CrosshairOverlay extends StatelessWidget {
  const _CrosshairOverlay();

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

class _BottomPanel extends StatelessWidget {
  final String statusText;
  final int pointCount;
  final bool isSaving;
  final bool canSave;
  final VoidCallback onSave;

  const _BottomPanel({
    required this.statusText,
    required this.pointCount,
    required this.isSaving,
    required this.canSave,
    required this.onSave,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
      decoration: BoxDecoration(
        color: const Color(0xFF0F172A).withValues(alpha: 0.95),
        border: const Border(
          top: BorderSide(color: Color(0xFF1E3A5F), width: 1),
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  statusText,
                  style: const TextStyle(color: Colors.white70, fontSize: 13),
                ),
                if (pointCount > 0) ...[
                  const SizedBox(height: 4),
                  Text(
                    '$pointCount ${pointCount == 1 ? 'point' : 'points'} placed',
                    style: TextStyle(
                      color: Colors.blue.shade300,
                      fontSize: 12,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 16),
          SizedBox(
            height: 48,
            child: ElevatedButton.icon(
              onPressed: (canSave && !isSaving) ? onSave : null,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.blue.shade700,
                disabledBackgroundColor: Colors.blue.shade900.withValues(
                  alpha: 0.4,
                ),
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: 20,
                  vertical: 12,
                ),
              ),
              icon: isSaving
                  ? const SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.save_outlined),
              label: Text(isSaving ? 'Saving…' : 'Save Area'),
            ),
          ),
        ],
      ),
    );
  }
}

enum _PointAction { move, delete, deselect }
