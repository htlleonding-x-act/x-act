import 'package:flutter/material.dart';

import '../xact_branding.dart';
import 'team_data.dart';

/// Compact overview row showing player counts per team / unassigned.
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
    final totalPlayers =
        spectatorCount + teams.fold<int>(0, (s, t) => s + t.players.length);

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: Row(
        children: [
          XActBranding.buildEyebrow('Teams · $totalPlayers players'),
          const Spacer(),
        ],
      ),
    );
  }
}
