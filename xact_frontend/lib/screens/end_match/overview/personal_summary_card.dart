import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

/// how the player on this device did
class PersonalSummaryCard extends StatelessWidget {
  const PersonalSummaryCard({
    super.key,
    required this.results,
    required this.member,
  });

  final GameResults results;
  final ResultMember member;

  @override
  Widget build(BuildContext context) {
    final team = results.teamById(member.teamId);
    final accent = teamAccent(results, member.teamId);
    final players = results.players;
    final rank =
        players
            .where((p) => p.stats.distanceMeters > member.stats.distanceMeters)
            .length +
        1;
    final stats = member.stats;

    return XActBranding.buildFormCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: accent.withValues(alpha: .25),
                child: Text(
                  initialOf(member.displayName),
                  style: XActText.subheading.copyWith(color: accent),
                ),
              ),
              const SizedBox(width: XActSpace.s3),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    XActBranding.buildEyebrow('Your match'),
                    Text(
                      '${member.displayName} · ${team?.teamName ?? 'No team'}',
                      style: XActText.subheading,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
              if (players.length > 1 && stats.distanceMeters > 0)
                Text(
                  '#$rank of ${players.length}',
                  style: XActText.mono.copyWith(fontSize: 16, color: accent),
                ),
            ],
          ),
          const SizedBox(height: XActSpace.s4),
          Wrap(
            spacing: XActSpace.s5,
            runSpacing: XActSpace.s3,
            children: [
              _metric('Distance', formatDistance(stats.distanceMeters)),
              _metric('Top speed', formatSpeed(stats.topSpeedKmh)),
              _metric('Pace', formatPace(stats.avgPaceSecondsPerKm)),
              if (stats.mrXTime > Duration.zero)
                _metric('As Mister X', formatShortDuration(stats.mrXTime)),
              if (stats.catchesMade > 0)
                _metric('Catches', '${stats.catchesMade}'),
              if (stats.outOfBounds > Duration.zero)
                _metric('Out of area', formatShortDuration(stats.outOfBounds)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _metric(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, style: XActText.caption),
        const SizedBox(height: 2),
        Text(value, style: XActText.bodySm.copyWith(fontWeight: FontWeight.w700)),
      ],
    );
  }
}
