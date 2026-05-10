import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

import '../xact_branding.dart';
import 'team_name_role_label.dart';

/// A team card displayed on the in-game Team tab.
class TeamCard extends StatelessWidget {
  final String teamName;
  final TeamRole? role;
  final Color color;
  final List<String> members;
  final bool isMisterX;

  const TeamCard({
    super.key,
    required this.teamName,
    required this.role,
    required this.color,
    required this.members,
    this.isMisterX = false,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: XActColors.surfaceAlt,
        borderRadius: XActRadius.md,
        border: Border.all(color: XActColors.hairline, width: 1),
      ),
      padding: const EdgeInsets.all(XActSpace.s4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: BoxDecoration(color: color, shape: BoxShape.circle),
              ),
              const SizedBox(width: XActSpace.s3),
              Flexible(
                fit: FlexFit.loose,
                child: TeamNameRoleLabel(
                  teamName: teamName,
                  role: role,
                  teamNameStyle: XActText.subheading.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              if (isMisterX) ...[
                const SizedBox(width: XActSpace.s2),
                const Icon(Icons.shield, color: XActColors.roleMrX, size: 18),
              ],
              const Spacer(),
              Row(
                children: [
                  Icon(Icons.people, color: XActColors.text3, size: 18),
                  const SizedBox(width: XActSpace.s1),
                  Text(
                    members.length.toString(),
                    style: XActText.body.copyWith(color: XActColors.text3),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s3),
          ...members.map(
            (member) => Padding(
              padding: const EdgeInsets.only(
                bottom: XActSpace.s1,
                left: XActSpace.s6,
              ),
              child: Text(
                member,
                style: XActText.body.copyWith(color: XActColors.text2),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
