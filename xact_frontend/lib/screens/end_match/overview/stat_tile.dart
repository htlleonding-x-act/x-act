import 'dart:math';

import 'package:flutter/material.dart';

import '../../../widgets/xact_branding.dart';

class StatTile extends StatelessWidget {
  const StatTile({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.accent,
  });

  final String label;
  final String value;
  final IconData icon;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(XActSpace.s4),
      decoration: BoxDecoration(
        color: XActColors.surface2,
        borderRadius: XActRadius.lg,
        border: Border.all(color: accent.withValues(alpha: .35)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 16, color: accent),
              const SizedBox(width: XActSpace.s2),
              Expanded(
                child: Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: XActText.caption.copyWith(color: XActColors.text3),
                ),
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s2),
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: XActText.title.copyWith(fontSize: 20),
          ),
        ],
      ),
    );
  }
}

/// lays tiles out in 2 columns on phones and 4 on wide screens
class StatTileGrid extends StatelessWidget {
  const StatTileGrid({super.key, required this.tiles});

  final List<Widget> tiles;

  static const double _gap = XActSpace.s3;
  static const double _wideBreakpoint = 640;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = constraints.maxWidth >= _wideBreakpoint ? 4 : 2;
        // a squeezed window can leave less room than the gaps, a negative
        // width would throw
        final width = max(
          0.0,
          (constraints.maxWidth - _gap * (columns - 1)) / columns,
        );

        return Wrap(
          spacing: _gap,
          runSpacing: _gap,
          children: [
            for (final tile in tiles) SizedBox(width: width, child: tile),
          ],
        );
      },
    );
  }
}
