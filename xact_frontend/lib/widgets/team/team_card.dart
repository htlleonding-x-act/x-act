import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

import '../xact_branding.dart';

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
    final roleColor = role != null ? XActColors.roleColor(role) : color;

    return Container(
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: roleColor.withValues(alpha: .25)),
        boxShadow: XActElevation.e1,
      ),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 10,
                height: 10,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: roleColor,
                  boxShadow: [
                    BoxShadow(
                      color: roleColor.withValues(alpha: .7),
                      blurRadius: 12,
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 10),
              Flexible(
                child: Text(
                  teamName,
                  style: XActText.subheading.copyWith(fontSize: 15),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: 8),
              XActBranding.buildRolePill(role: role),
              const Spacer(),
              Text(
                '${members.length}',
                style: XActText.bodySm.copyWith(
                  color: roleColor,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          if (members.isEmpty)
            Padding(
              padding: const EdgeInsets.only(left: 4),
              child: Text(
                'No players yet.',
                style: XActText.caption.copyWith(
                  color: XActColors.text4,
                  fontSize: 13,
                ),
              ),
            )
          else
            ...members.map(
              (member) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 3),
                child: Row(
                  children: [
                    Container(
                      width: 30,
                      height: 30,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        gradient: LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: [
                            roleColor,
                            roleColor.withValues(alpha: .6),
                          ],
                        ),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        member.isEmpty ? '?' : member[0].toUpperCase(),
                        style: XActText.bodySm.copyWith(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          color: Colors.white,
                        ),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        member,
                        style: XActText.bodySm,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}
