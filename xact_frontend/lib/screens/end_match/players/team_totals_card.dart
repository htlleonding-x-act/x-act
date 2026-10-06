import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

class TeamTotalsCard extends StatelessWidget {
  const TeamTotalsCard({
    super.key,
    required this.team,
    required this.accent,
    required this.isWinner,
  });

  final ResultTeam team;
  final Color accent;
  final bool isWinner;

  @override
  Widget build(BuildContext context) {
    final totals = team.totals;

    return Container(
      width: 220,
      padding: const EdgeInsets.all(XActSpace.s4),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        border: Border.all(color: accent.withValues(alpha: .45)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 10,
                height: 10,
                decoration: BoxDecoration(color: accent, shape: BoxShape.circle),
              ),
              const SizedBox(width: XActSpace.s2),
              Expanded(
                child: Text(
                  team.teamName,
                  style: XActText.subheading,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              if (isWinner)
                const Icon(
                  Icons.emoji_events_rounded,
                  size: 18,
                  color: XActColors.warning,
                ),
            ],
          ),
          const SizedBox(height: XActSpace.s3),
          _row('Distance', formatDistance(totals.distanceMeters)),
          _row('Top speed', formatSpeed(totals.topSpeedKmh)),
          _row('As Mister X', formatShortDuration(totals.mrXTime)),
          _row('Catches', '${totals.catchesMade}'),
          if (totals.powerUpsUsed > 0) _row('Power-ups', '${totals.powerUpsUsed}'),
        ],
      ),
    );
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Expanded(child: Text(label, style: XActText.caption)),
          Text(value, style: XActText.bodySm.copyWith(fontWeight: FontWeight.w700)),
        ],
      ),
    );
  }
}
