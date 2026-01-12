import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'map_header.dart';
import 'map_legend.dart';

class MapArea extends StatefulWidget {
  const MapArea({super.key});

  @override
  State<MapArea> createState() => _MapAreaState();
}

class _MapAreaState extends State<MapArea> {
  final MapController _mapController = MapController();

  static const LatLng _centerLocation = LatLng(48.3069, 14.2858);

  final List<PlayerMarker> _players = [
    PlayerMarker(
      id: 'player1',
      name: 'You',
      position: const LatLng(48.3069, 14.2858),
      color: Colors.blue,
      isCurrentUser: true,
    ),
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
  Widget build(BuildContext context) {
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
              initialCenter: _centerLocation,
              initialZoom: 15.0,
              minZoom: 10.0,
              maxZoom: 18.0,
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
              MarkerLayer(
                markers: _players
                    .map((player) => _buildMarker(player))
                    .toList(),
              ),
            ],
          ),
          const MapHeader(),
          const MapLegend(),
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
                _ZoomButton(
                  icon: Icons.my_location,
                  onPressed: () {
                    _mapController.move(_centerLocation, 15.0);
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
