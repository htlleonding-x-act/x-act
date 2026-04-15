import 'package:flutter/material.dart';

import '../xact_branding.dart';
import 'team_data.dart';

/// Static (non-draggable) content for a player tile.
class PlayerTileContent extends StatelessWidget {
  final LobbyPlayer player;
  final Color dotColor;

  const PlayerTileContent({
    super.key,
    required this.player,
    required this.dotColor,
  });

  @override
  Widget build(BuildContext context) {
    final isYou = player.isCurrentUser;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: XActBranding.backgroundColor,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.drag_indicator, color: Colors.white24, size: 20),
          const SizedBox(width: 8),
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(color: dotColor, shape: BoxShape.circle),
          ),
          const SizedBox(width: 10),
          Text(
            player.name,
            style: const TextStyle(color: Colors.white, fontSize: 14),
          ),
          if (isYou) ...[
            const SizedBox(width: 6),
            const Text(
              '(you)',
              style: TextStyle(color: Colors.amber, fontSize: 13),
            ),
          ],
        ],
      ),
    );
  }
}

/// Draggable player tile – can be long-press-dragged into any team or spectators.
class DraggablePlayerTile extends StatelessWidget {
  final LobbyPlayer player;
  final Color dotColor;

  const DraggablePlayerTile({
    super.key,
    required this.player,
    required this.dotColor,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: LongPressDraggable<LobbyPlayer>(
        data: player,
        delay: const Duration(milliseconds: 150),
        feedback: Material(
          color: Colors.transparent,
          child: Opacity(
            opacity: 0.85,
            child: SizedBox(
              width: MediaQuery.of(context).size.width - 64,
              child: PlayerTileContent(player: player, dotColor: dotColor),
            ),
          ),
        ),
        childWhenDragging: Padding(
          padding: const EdgeInsets.symmetric(vertical: 2),
          child: Container(
            height: 40,
            decoration: BoxDecoration(
              color: Colors.white10,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: Colors.white12),
            ),
          ),
        ),
        child: PlayerTileContent(player: player, dotColor: dotColor),
      ),
    );
  }
}
