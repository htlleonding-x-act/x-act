import 'package:flutter/material.dart';
import 'package:xact_frontend/api/models.dart';

import '../xact_branding.dart';
import 'draggable_player_tile.dart';
import 'team_data.dart';

/// Drag-target card that holds the unassigned players list.
class SpectatorsCard extends StatelessWidget {
  final List<LobbyPlayer> spectators;
  final ValueChanged<LobbyPlayer> onPlayerDropped;

  const SpectatorsCard({
    super.key,
    required this.spectators,
    required this.onPlayerDropped,
  });

  @override
  Widget build(BuildContext context) {
    final color = XActColors.roleSpectator;
    return DragTarget<LobbyPlayer>(
      onWillAcceptWithDetails: (_) => true,
      onAcceptWithDetails: (details) => onPlayerDropped(details.data),
      builder: (context, candidateData, rejectedData) {
        final isHovering = candidateData.isNotEmpty;
        return AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          width: double.infinity,
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: isHovering
                ? color.withValues(alpha: .10)
                : XActColors.surface,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(
              color: isHovering ? color : color.withValues(alpha: .22),
              width: isHovering ? 2 : 1,
            ),
            boxShadow: XActElevation.e1,
          ),
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
                      color: color,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Text(
                    'Unassigned',
                    style: XActText.subheading.copyWith(fontSize: 15),
                  ),
                  const SizedBox(width: 8),
                  XActBranding.buildRolePill(role: TeamRole.spectator),
                  const Spacer(),
                  Text(
                    '${spectators.length}',
                    style: XActText.bodySm.copyWith(
                      color: color,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              if (spectators.isEmpty)
                Padding(
                  padding: const EdgeInsets.only(left: 4, top: 2, bottom: 2),
                  child: Text(
                    'No players waiting.',
                    style: XActText.caption.copyWith(
                      color: XActColors.text4,
                      fontSize: 13,
                    ),
                  ),
                )
              else
                ...spectators.map(
                  (player) => DraggablePlayerTile(
                    player: player,
                    dotColor: color,
                  ),
                ),
            ],
          ),
        );
      },
    );
  }
}
