import 'package:flutter/material.dart';

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
    return DragTarget<LobbyPlayer>(
      onWillAcceptWithDetails: (_) => true,
      onAcceptWithDetails: (details) => onPlayerDropped(details.data),
      builder: (context, candidateData, rejectedData) {
        final isHovering = candidateData.isNotEmpty;
        return AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          width: double.infinity,
          padding: const EdgeInsets.all(XActSpace.s3),
          decoration: BoxDecoration(
            color: isHovering
                ? XActColors.roleSpectator.withValues(alpha: .12)
                : XActColors.surface,
            borderRadius: XActRadius.md,
            border: Border.all(
              color: isHovering
                  ? XActColors.roleSpectator
                  : XActColors.roleSpectator.withValues(alpha: .47),
              width: isHovering ? 2.5 : 1.5,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Text(
                    'Unassigned',
                    style: XActText.body.copyWith(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(width: XActSpace.s2),
                  Icon(
                    Icons.visibility,
                    color: XActColors.text4,
                    size: 18,
                  ),
                  const Spacer(),
                  Text(
                    '${spectators.length}/∞',
                    style: XActText.caption.copyWith(
                      color: XActColors.text3,
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: XActSpace.s2),
              ...spectators.map(
                (player) => DraggablePlayerTile(
                  player: player,
                  dotColor: XActColors.roleSpectator,
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
