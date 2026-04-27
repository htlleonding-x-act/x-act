import 'package:flutter/material.dart';

import '../xact_branding.dart';
import 'draggable_player_tile.dart';
import 'team_data.dart';

/// Drag-target card that holds the spectators list.
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
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: isHovering
                ? Colors.blueAccent.withAlpha(30)
                : XActBranding.cardColor,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: isHovering
                  ? Colors.blueAccent
                  : Colors.blueAccent.withAlpha(120),
              width: isHovering ? 2.5 : 1.5,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Text(
                    'Spectators',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(width: 8),
                  const Icon(Icons.visibility, color: Colors.white38, size: 18),
                  const Spacer(),
                  Text(
                    '${spectators.length}/∞',
                    style: const TextStyle(color: Colors.white54, fontSize: 13),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              ...spectators.map(
                (player) =>
                    DraggablePlayerTile(player: player, dotColor: Colors.grey),
              ),
            ],
          ),
        );
      },
    );
  }
}
