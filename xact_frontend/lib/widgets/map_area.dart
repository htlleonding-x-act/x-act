import 'package:flutter/material.dart';
import 'map_header.dart';
import 'map_legend.dart';

class MapArea extends StatelessWidget {
  const MapArea({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF1E293B),
        border: Border(bottom: BorderSide(color: Colors.blue.shade700, width: 2)),
      ),
      child: Stack(
        children: [
          Center(
            child: Text('MAP AREA', style: Theme.of(context).textTheme.headlineMedium?.copyWith(color: Colors.white54)),
          ),
          const MapHeader(),
          const MapLegend(),
        ],
      ),
    );
  }
}
