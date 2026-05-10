import 'package:flutter/material.dart';

import 'xact_branding.dart';

final class MapLegendTeamEntry {
  final String label;
  final Color color;

  const MapLegendTeamEntry({required this.label, required this.color});
}

class MapLegend extends StatelessWidget {
  final List<MapLegendTeamEntry> teamEntries;
  final Color myLocationColor;

  const MapLegend({
    super.key,
    required this.teamEntries,
    required this.myLocationColor,
  });

  @override
  Widget build(BuildContext context) {
    final items = <Widget>[
      _LegendItem(color: myLocationColor, label: 'Your Location'),
    ];

    for (final entry in teamEntries) {
      items.add(const SizedBox(height: XActSpace.s2));
      items.add(_LegendItem(color: entry.color, label: entry.label));
    }

    return Positioned(
      bottom: XActSpace.s4,
      left: XActSpace.s4,
      child: Container(
        padding: const EdgeInsets.all(XActSpace.s3),
        decoration: BoxDecoration(
          color: XActColors.surfaceDeep.withValues(alpha: .9),
          borderRadius: XActRadius.sm,
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
        const SizedBox(width: XActSpace.s2),
        Text(label, style: XActText.bodySm),
      ],
    );
  }
}
