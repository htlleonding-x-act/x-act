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
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: isHovering
                ? team.color.withAlpha(30)
                : XActBranding.cardColor,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: isHovering ? team.color : team.color.withAlpha(150),
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
                      teamNameStyle: const TextStyle(
                        color: Colors.white,
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                      ),
                      roleStyle: const TextStyle(
                        color: Color(0xFFBFDBFE),
                        fontSize: 14,
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
                    const SizedBox(width: 10),
                    Text(
                      '${team.players.length}/${team.maxPlayers}',
                      style: TextStyle(color: team.color, fontSize: 14),
                    ),
                    if (team.isDeletable) ...[
                      const SizedBox(width: 10),
                      GestureDetector(
                        onTap: onDelete,
                        child: const Icon(
                          Icons.delete,
                          color: Colors.white38,
                          size: 18,
                        ),
                      ),
                    ],
                  ] else
                    Text(
                      '${team.players.length}/${team.maxPlayers}',
                      style: TextStyle(color: team.color, fontSize: 14),
                    ),
                ],
              ),
              const SizedBox(height: 8),
              if (team.players.isEmpty)
                const Text(
                  'No players',
                  style: TextStyle(color: Colors.white38, fontSize: 13),
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
