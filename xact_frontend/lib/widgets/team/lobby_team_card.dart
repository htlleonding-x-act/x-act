import 'package:flutter/material.dart';

import '../xact_branding.dart';
import 'draggable_player_tile.dart';
import 'team_data.dart';

/// Drag-target card for a single team in the lobby.
class LobbyTeamCard extends StatelessWidget {
  final TeamData team;
  final bool isLeader;
  final VoidCallback? onRename;
  final VoidCallback? onDelete;
  final ValueChanged<LobbyPlayer> onPlayerDropped;

  const LobbyTeamCard({
    super.key,
    required this.team,
    required this.isLeader,
    this.onRename,
    this.onDelete,
    required this.onPlayerDropped,
  });

  @override
  Widget build(BuildContext context) {
    final roleColor = XActColors.roleColor(team.role);

    return DragTarget<LobbyPlayer>(
      onWillAcceptWithDetails: (details) {
        return team.players.length < team.maxPlayers &&
            !team.players.any((p) => p.memberId == details.data.memberId);
      },
      onAcceptWithDetails: (details) => onPlayerDropped(details.data),
      builder: (context, candidateData, rejectedData) {
        final isHovering = candidateData.isNotEmpty;
        return AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          width: double.infinity,
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: isHovering
                ? roleColor.withValues(alpha: .10)
                : XActColors.surface,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(
              color: isHovering
                  ? roleColor
                  : roleColor.withValues(alpha: .25),
              width: isHovering ? 2 : 1,
            ),
            boxShadow: XActElevation.e1,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Row(
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
                            team.name.trim().isEmpty
                                ? 'Team'
                                : team.name.trim(),
                            style: XActText.subheading.copyWith(fontSize: 15),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 8),
                        XActBranding.buildRolePill(role: team.role),
                      ],
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    '${team.players.length}/${team.maxPlayers}',
                    style: XActText.bodySm.copyWith(
                      color: roleColor,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  if (isLeader) ...[
                    const SizedBox(width: 10),
                    _IconAction(
                      icon: Icons.edit_rounded,
                      color: roleColor,
                      onTap: onRename,
                    ),
                    if (team.isDeletable) ...[
                      const SizedBox(width: 6),
                      _IconAction(
                        icon: Icons.delete_outline_rounded,
                        color: XActColors.text4,
                        onTap: onDelete,
                      ),
                    ],
                  ],
                ],
              ),
              const SizedBox(height: 12),
              if (team.players.isEmpty)
                Padding(
                  padding: const EdgeInsets.only(left: 4, top: 2, bottom: 2),
                  child: Text(
                    'No players yet — drag someone in.',
                    style: XActText.caption.copyWith(
                      color: XActColors.text4,
                      fontSize: 13,
                    ),
                  ),
                )
              else
                ...team.players.map(
                  (p) => DraggablePlayerTile(player: p, dotColor: roleColor),
                ),
            ],
          ),
        );
      },
    );
  }
}

class _IconAction extends StatelessWidget {
  final IconData icon;
  final Color color;
  final VoidCallback? onTap;

  const _IconAction({
    required this.icon,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkResponse(
      onTap: onTap,
      radius: 18,
      child: Icon(icon, color: color, size: 18),
    );
  }
}
