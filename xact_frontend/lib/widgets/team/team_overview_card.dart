import 'package:flutter/material.dart';

import '../xact_branding.dart';
import 'team_data.dart';

/// Card showing an overview of all teams and spectators with color-coded chips.
class TeamOverviewCard extends StatelessWidget {
  final int spectatorCount;
  final List<TeamData> teams;

  const TeamOverviewCard({
    super.key,
    required this.spectatorCount,
    required this.teams,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.people, color: Colors.white70, size: 18),
              SizedBox(width: 6),
              Text(
                'Team Overview',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 16,
            runSpacing: 6,
            children: [
              _overviewChip('Spectators', Colors.grey, '$spectatorCount/∞'),
              ...teams.map(
                (t) => _overviewChip(
                  t.name,
                  t.color,
                  '${t.players.length}/${t.maxPlayers}',
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _overviewChip(String name, Color color, String count) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text(name, style: const TextStyle(color: Colors.white70, fontSize: 13)),
        const SizedBox(width: 4),
        Text(
          count,
          style: const TextStyle(color: Colors.white38, fontSize: 13),
        ),
      ],
    );
  }
}
