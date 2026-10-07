import 'package:flutter/material.dart';

import '../../../../api/game_results.dart';
import '../../../../api/models.dart';
import '../../../../widgets/xact_branding.dart';
import '../../end_match_format.dart';

final class TimelineEventStyle {
  const TimelineEventStyle({
    required this.icon,
    required this.color,
    required this.title,
    this.subtitle,
  });

  final IconData icon;
  final Color color;
  final String title;
  final String? subtitle;

  factory TimelineEventStyle.of(GameResults results, TimelineEvent event) {
    final team = results.teamById(event.teamId)?.teamName ?? 'A team';
    final member = results.memberById(event.memberId)?.displayName;

    return switch (event.type) {
      TimelineEventType.gameStarted => const TimelineEventStyle(
        icon: Icons.flag_rounded,
        color: XActColors.success,
        title: 'Match started',
      ),
      TimelineEventType.mrXRevealed => TimelineEventStyle(
        icon: Icons.location_on_rounded,
        color: XActColors.roleMrX,
        title: 'Mister X revealed',
        subtitle: member == null ? team : '$member · $team',
      ),
      TimelineEventType.mrXCaught => TimelineEventStyle(
        icon: Icons.back_hand_rounded,
        color: XActColors.warning,
        title: '$team caught Mister X',
        subtitle: [
          if (member != null) 'by $member',
          if (results.teamById(event.otherTeamId) case final caught?)
            'from ${caught.teamName}',
        ].join(' '),
      ),
      TimelineEventType.powerUpUsed => TimelineEventStyle(
        icon: event.powerUpType == PowerUpType.doubleMove
            ? Icons.fast_forward_rounded
            : Icons.confirmation_number_rounded,
        color: const Color(0xFF38BDF8),
        title:
            '${member ?? team} used ${event.powerUpType == PowerUpType.doubleMove ? 'Double move' : 'Black ticket'}',
      ),
      TimelineEventType.leftGameArea => TimelineEventStyle(
        icon: Icons.wrong_location_rounded,
        color: const Color(0xFFC084FC),
        title: '${member ?? team} left the game area',
        subtitle: event.duration == null
            ? null
            : 'for ${formatShortDuration(event.duration!)}',
      ),
      TimelineEventType.gameEnded || null => const TimelineEventStyle(
        icon: Icons.sports_score_rounded,
        color: XActColors.success,
        title: 'Match ended',
      ),
    };
  }
}
