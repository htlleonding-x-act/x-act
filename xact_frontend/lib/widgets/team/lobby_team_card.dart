import 'package:flutter/material.dart';

import '../xact_branding.dart';
import 'draggable_player_tile.dart';
import 'team_data.dart';
import 'team_name_role_label.dart';

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
          padding: const EdgeInsets.all(XActSpace.s3),
          decoration: BoxDecoration(
            color: isHovering
                ? team.color.withValues(alpha: .12)
                : XActColors.surface,
            borderRadius: XActRadius.md,
            border: Border.all(
              color: isHovering
                  ? team.color
                  : team.color.withValues(alpha: .58),
              width: isHovering ? 2.5 : 1.5,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: TeamNameRoleLabel(
                      teamName: team.name,
                      role: team.role,
                      teamNameStyle: XActText.body.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (isLeader) ...[
                    GestureDetector(
                      onTap: onRename,
                      child: Icon(Icons.edit, color: team.color, size: 18),
                    ),
                    const SizedBox(width: XActSpace.s3),
                    Text(
                      '${team.players.length}/${team.maxPlayers}',
                      style: XActText.bodySm.copyWith(color: team.color),
                    ),
                    if (team.isDeletable) ...[
                      const SizedBox(width: XActSpace.s3),
                      GestureDetector(
                        onTap: onDelete,
                        child: Icon(
                          Icons.delete,
                          color: XActColors.text4,
                          size: 18,
                        ),
                      ),
                    ],
                  ] else
                    Text(
                      '${team.players.length}/${team.maxPlayers}',
                      style: XActText.bodySm.copyWith(color: team.color),
                    ),
                ],
              ),
              const SizedBox(height: XActSpace.s2),
              if (team.players.isEmpty)
                Text(
                  'No players',
                  style: XActText.caption.copyWith(
                    color: XActColors.text4,
                    fontSize: 13,
                  ),
                )
              else
                ...team.players.map(
                  (p) => DraggablePlayerTile(player: p, dotColor: team.color),
                ),
            ],
          ),
        );
      },
    );
  }
}
