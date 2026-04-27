import 'package:flutter/material.dart';

final class MapLegendTeamEntry {
  final String label;
  final Color color;

  const MapLegendTeamEntry({required this.label, required this.color});
}

class MapLegend extends StatelessWidget {
  final List<MapLegendTeamEntry> teamEntries;

  const MapLegend({super.key, required this.teamEntries});

  @override
  Widget build(BuildContext context) {
    final items = <Widget>[
      _LegendItem(color: Colors.blue, label: 'Your Location'),
    ];

    for (final entry in teamEntries) {
      items.add(const SizedBox(height: 8));
      items.add(_LegendItem(color: entry.color, label: entry.label));
    }

    return Positioned(
      bottom: 16,
      left: 16,
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: const Color(0xFF0F172A).withValues(alpha: 0.9),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: items,
        ),
      ),
    );
  }
}

class _LegendItem extends StatelessWidget {
  final Color color;
  final String label;

  const _LegendItem({required this.color, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 12,
          height: 12,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 8),
        Text(label, style: const TextStyle(color: Colors.white)),
      ],
    );
  }
}
