import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../constants.dart';
import '../../services/geofence_store.dart';
import '../../services/location_service.dart';
import '../../widgets/lobby/define_game_area_widgets.dart';
import '../../widgets/xact_branding.dart';

class DefineGameAreaScreen extends StatefulWidget {
  final int sessionId;
  final String gameName;
  final List<LatLng>? initialPoints;
  final bool fromLobby;

  const DefineGameAreaScreen({
    super.key,
    required this.sessionId,
    required this.gameName,
    this.initialPoints,
    this.fromLobby = false,
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

  // Fallback used only when GPS is unavailable / denied.
  static const LatLng _fallbackCenter = kFallbackMapCenter;

  // Resolved initial center – null while GPS is being acquired.
  LatLng? _initialCenter;

  // Current user location shown as a simple symbol on the map.
  LatLng? _myLocation;

  // True while waiting for the first GPS fix on screen open.
  bool _isLocating = true;

  // True when permission was denied and we had to fall back to [_fallbackCenter].
  bool _usedFallback = false;

  @override
  void initState() {
    super.initState();
    if (widget.initialPoints != null && widget.initialPoints!.isNotEmpty) {
      _points.addAll(widget.initialPoints!);
    }
    _resolveInitialCenter();
  }

  /// Tries to resolve the map's initial center from the device GPS.
  /// Falls back to [_fallbackCenter] when permission is denied, location
  /// services are disabled, or the lookup times out.
  Future<void> _resolveInitialCenter() async {
    final cached = LocationService.instance.lastKnownPosition;
    if (cached != null) {
      if (!mounted) return;
      setState(() {
        _initialCenter = LatLng(cached.latitude, cached.longitude);
        _myLocation = LatLng(cached.latitude, cached.longitude);
        _isLocating = false;
      });
      return;
    }

    final position = await LocationService.instance.getCurrentPosition();
    if (!mounted) return;

    if (position != null) {
      setState(() {
        _initialCenter = LatLng(position.latitude, position.longitude);
        _myLocation = LatLng(position.latitude, position.longitude);
        _isLocating = false;
      });
    } else {
      setState(() {
        _initialCenter = _fallbackCenter;
        _myLocation = null;
        _isLocating = false;
        _usedFallback = true;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Could not detect your location – using default area. '
            'Enable location permission to center the map on you.',
          ),
          backgroundColor: Colors.orange,
          duration: Duration(seconds: 4),
        ),
      );
    }
  }

  static const int _maxPoints = 10;

  void _onMapTap(TapPosition _, LatLng point) {
    if (_isMoveMode && _selectedIndex >= 0 && _selectedIndex < _points.length) {
      setState(() {
        _points[_selectedIndex] = point;
        _isMoveMode = false;
      });
      return;
    }

    if (_points.length >= _maxPoints) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Maximum of $_maxPoints corners reached.'),
          backgroundColor: Colors.orange,
        ),
      );
      return;
    }

    setState(() {
      _points.insert(_bestInsertIndex(point), point);
      _selectedIndex = -1;
      _isMoveMode = false;
    });
  }

  // Returns the index at which [point] should be inserted so that it lands
  // between the two existing corners that form the closest edge. This keeps
  // the polygon shape natural instead of always appending chronologically.
  int _bestInsertIndex(LatLng point) {
    final n = _points.length;
    if (n < 3) return n;

    var bestIndex = n;
    var bestCost = double.infinity;
    for (var i = 0; i < n; i++) {
      final a = _points[i];
      final b = _points[(i + 1) % n];
      final cost =
          _planarDistance(point, a) +
          _planarDistance(point, b) -
          _planarDistance(a, b);
      if (cost < bestCost) {
        bestCost = cost;
        bestIndex = i + 1;
      }
    }
    return bestIndex;
  }

  double _planarDistance(LatLng a, LatLng b) {
    final dLat = a.latitude - b.latitude;
    final dLng = a.longitude - b.longitude;
    return math.sqrt(dLat * dLat + dLng * dLng);
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
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Yes', style: TextStyle(color: Colors.red)),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'No',
              style: TextStyle(color: Colors.white54),
            ),
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
    if (_points.length >= _maxPoints) {
      return '$_maxPoints corners placed – maximum reached';
    }
    return '${_points.length} corners - tap a corner to select or tap map to add';
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
              if (!widget.fromLobby) ...[
                XActBranding.buildEyebrow('Step 2 of 3'),
                const SizedBox(height: 2),
              ],
              Text(
                widget.fromLobby ? 'Edit play area' : 'Define play area',
                style: XActText.heading,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
        actions: [
          if (_points.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.undo_rounded),
              color: XActColors.text2,
              tooltip: 'Undo last corner',
              onPressed: _undoLast,
            ),
          if (_points.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.delete_sweep_outlined),
              color: XActColors.text2,
              tooltip: 'Clear all',
              onPressed: _clearAll,
            ),
          const SizedBox(width: 4),
        ],
      ),
      body: _isLocating ? _buildLocatingView() : _buildMapView(),
    );
  }

  Widget _buildLocatingView() {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const CircularProgressIndicator(color: XActColors.secondary),
          const SizedBox(height: 16),
          Text(
            'Detecting your location…',
            style: XActText.bodySm.copyWith(color: XActColors.text2),
          ),
        ],
      ),
    );
  }

  Widget _buildMapView() {
    return Stack(
      children: [
        DefineGameAreaMap(
          mapController: _mapController,
          fallbackCenter: _initialCenter ?? _fallbackCenter,
          myLocation: _myLocation,
          points: _points,
          selectedIndex: _selectedIndex,
          isMoveMode: _isMoveMode,
          onMapTap: _onMapTap,
          onPointTapDown: (index) =>
              (details) => _showPointMenu(index, details.globalPosition),
        ),

        // ── Crosshair hint overlay ──────────────────────────────
        const Center(child: DefineGameAreaCrosshairOverlay()),

        // ── Fallback notice when GPS was unavailable ────────────
        if (_usedFallback)
          Positioned(
            top: 8,
            left: 12,
            right: 12,
            child: Material(
              color: Colors.transparent,
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 10,
                ),
                decoration: BoxDecoration(
                  color: XActColors.warning.withValues(alpha: .18),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(
                    color: XActColors.warning.withValues(alpha: .35),
                  ),
                ),
                child: Row(
                  children: [
                    const Icon(
                      Icons.location_off_rounded,
                      color: XActColors.warning,
                      size: 18,
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'Using default location – enable GPS to center on you.',
                        style: XActText.caption.copyWith(
                          color: XActColors.text1,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

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
    );
  }
}

enum _PointAction { move, delete, deselect }
