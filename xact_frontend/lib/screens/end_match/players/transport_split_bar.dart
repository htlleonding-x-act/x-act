import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../api/models.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

/// how a player's time splits across transport modes. hidden while everybody
/// walks, a bar that is all foot says nothing
class TransportSplitBar extends StatelessWidget {
  const TransportSplitBar({super.key, required this.stats});

  final MemberStats stats;

  static Color colorOf(TransportMode mode) => switch (mode) {
    TransportMode.foot => XActColors.success,
    TransportMode.bus => const Color(0xFF38BDF8),
    TransportMode.tram => XActColors.warning,
    TransportMode.train => const Color(0xFFC084FC),
  };

  static String labelOf(TransportMode mode) => switch (mode) {
    TransportMode.foot => 'Foot',
    TransportMode.bus => 'Bus',
    TransportMode.tram => 'Tram',
    TransportMode.train => 'Train',
  };

  @override
  Widget build(BuildContext context) {
    if (!stats.usedTransit) {
      return const SizedBox.shrink();
    }

    final entries = stats.timeByMode.entries
        .where((e) => e.value > Duration.zero)
        .toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ClipRRect(
          borderRadius: XActRadius.pill,
          child: SizedBox(
            height: 8,
            child: Row(
              children: [
                for (final entry in entries)
                  Expanded(
                    flex: entry.value.inSeconds,
                    child: ColoredBox(color: colorOf(entry.key)),
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: XActSpace.s2),
        Wrap(
          spacing: XActSpace.s3,
          children: [
            for (final entry in entries)
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: colorOf(entry.key),
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: XActSpace.s1),
                  Text(
                    '${labelOf(entry.key)} ${formatShortDuration(entry.value)}',
                    style: XActText.caption,
                  ),
                ],
              ),
          ],
        ),
      ],
    );
  }
}
