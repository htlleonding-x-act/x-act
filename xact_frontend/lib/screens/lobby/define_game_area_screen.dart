import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../services/geofence_store.dart';
import '../../services/location_service.dart';
import '../../widgets/lobby/define_game_area_widgets.dart';

class DefineGameAreaScreen extends StatefulWidget {
  final int sessionId;
  final String gameName;

  const DefineGameAreaScreen({
    super.key,
    required this.sessionId,
    required this.gameName,
  });

  @override
  State<DefineGameAreaScreen> createState() => _DefineGameAreaScreenState();
}

class _DefineGameAreaScreenState extends State<DefineGameAreaScreen> {
  final MapController _mapController = MapController();

  // Corner markers the host has placed - these form the polygon.
  final List<LatLng> _points = [];

  // Index of the currently selected corner marker, -1 when none is selected.
  int _selectedIndex = -1;

  // When true, the next map tap repositions the selected corner marker.
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
            title: Text('Move corner', style: TextStyle(color: Colors.white)),
          ),
        ),
        PopupMenuItem<_PointAction>(
          value: _PointAction.delete,
          child: ListTile(
            dense: true,
            leading: Icon(Icons.delete_outline, color: Colors.redAccent),
            title: Text('Delete corner', style: TextStyle(color: Colors.white)),
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
          'Delete corner ${index + 1}?',
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
          'This will remove all placed corners.',
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
          content: Text('Place at least 3 corners to define the game area.'),
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
      return 'Move mode: tap on map to set corner ${_selectedIndex + 1}';
    }
    if (_selectedIndex >= 0) {
      return 'Corner ${_selectedIndex + 1} selected · choose action from menu';
    }
    if (_points.length == 1) return '1 corner placed - add at least 2 more';
    if (_points.length == 2) return '2 corners placed - add at least 1 more';
    return '${_points.length} corners - tap a corner to select or tap map to add';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1A1F2E),
      appBar: AppBar(
        backgroundColor: const Color(0xFF0F172A),
        foregroundColor: Colors.white,
        title: Text('Define ${widget.gameName} Game Area'),
        actions: [
          if (_points.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.undo),
              tooltip: 'Undo last corner',
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
          DefineGameAreaMap(
            mapController: _mapController,
            fallbackCenter: _fallbackCenter,
            points: _points,
            selectedIndex: _selectedIndex,
            isMoveMode: _isMoveMode,
            onMapTap: _onMapTap,
            onPointTapDown: (index) =>
                (details) => _showPointMenu(index, details.globalPosition),
          ),

          // ── Crosshair hint overlay ──────────────────────────────
          const Center(child: DefineGameAreaCrosshairOverlay()),

          // ── Status bar at bottom ────────────────────────────────
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: DefineGameAreaBottomPanel(
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

enum _PointAction { move, delete, deselect }
