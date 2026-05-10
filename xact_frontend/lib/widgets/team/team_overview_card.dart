import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

import '../xact_branding.dart';
import 'team_data.dart';
import 'team_name_role_label.dart';

/// Card showing an overview of all teams and unassigned players with color-coded chips.
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
      padding: const EdgeInsets.all(XActSpace.s3),
      decoration: const BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.md,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.people, color: XActColors.text2, size: 18),
              const SizedBox(width: XActSpace.s1),
              Text(
                'Team Overview',
                style: XActText.bodySm.copyWith(
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s2),
          Wrap(
            spacing: XActSpace.s4,
            runSpacing: XActSpace.s1,
            children: [
              _overviewChip(
                'Unassigned',
                XActColors.roleSpectator,
                '$spectatorCount/∞',
              ),
              ...teams.map(
                (t) => _overviewChip(
                  t.name,
                  t.color,
                  '${t.players.length}/${t.maxPlayers}',
                  role: t.role,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _overviewChip(
    String name,
    Color color,
    String count, {
    TeamRole? role,
  }) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: XActSpace.s1),
        TeamNameRoleLabel(
          teamName: name,
          role: role,
          teamNameStyle: XActText.caption.copyWith(
            color: XActColors.text2,
            fontSize: 13,
          ),
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
        const SizedBox(width: XActSpace.s1),
        Text(
          count,
          style: XActText.caption.copyWith(
            color: XActColors.text4,
            fontSize: 13,
          ),
        ),
      ],
    );
  }
}
