import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

final class AwardStyle {
  const AwardStyle({
    required this.title,
    required this.description,
    required this.icon,
    required this.color,
    required this.formatValue,
  });

  final String title;
  final String description;
  final IconData icon;
  final Color color;
  final String Function(double value) formatValue;

  static AwardStyle of(AwardType type) {
    return switch (type) {
      AwardType.marathon => AwardStyle(
        title: 'Marathon',
        description: 'Covered the most ground',
        icon: Icons.directions_walk_rounded,
        color: XActColors.success,
        formatValue: formatDistance,
      ),
      AwardType.speedDemon => AwardStyle(
        title: 'Speed demon',
        description: 'Fastest sprint of the match',
        icon: Icons.bolt_rounded,
        color: XActColors.warning,
        formatValue: formatSpeed,
      ),
      AwardType.hunter => AwardStyle(
        title: 'Hunter',
        description: 'Caught Mister X',
        icon: Icons.back_hand_rounded,
        color: XActColors.secondary,
        formatValue: (v) => v.round() == 1 ? '1 catch' : '${v.round()} catches',
      ),
      AwardType.ghost => AwardStyle(
        title: 'Ghost',
        description: 'Longest run as Mister X',
        icon: Icons.visibility_off_rounded,
        color: XActColors.roleMrX,
        formatValue: (v) => formatShortDuration(Duration(seconds: v.round())),
      ),
      AwardType.transitPro => AwardStyle(
        title: 'Public transport pro',
        description: 'Most time on bus, tram and train',
        icon: Icons.directions_bus_rounded,
        color: const Color(0xFF38BDF8),
        formatValue: (v) => formatShortDuration(Duration(seconds: v.round())),
      ),
      AwardType.ruleBender => AwardStyle(
        title: 'Rule bender',
        description: 'Spent the most time outside the game area',
        icon: Icons.wrong_location_rounded,
        color: const Color(0xFFC084FC),
        formatValue: (v) => formatShortDuration(Duration(seconds: v.round())),
      ),
    };
  }
}
