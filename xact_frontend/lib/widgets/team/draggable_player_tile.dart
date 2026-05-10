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
      padding: const EdgeInsets.symmetric(
        horizontal: XActSpace.s3,
        vertical: XActSpace.s2 + 2,
      ),
      decoration: const BoxDecoration(
        color: XActColors.bg,
        borderRadius: XActRadius.sm,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.drag_indicator, color: XActColors.text5, size: 20),
          const SizedBox(width: XActSpace.s2),
          Container(
            width: 10,
            height: 10,
            decoration: BoxDecoration(color: dotColor, shape: BoxShape.circle),
          ),
          const SizedBox(width: XActSpace.s2 + 2),
          Text(player.name, style: XActText.bodySm),
          if (isYou) ...[
            const SizedBox(width: XActSpace.s1 + 2),
            Text(
              '(you)',
              style: XActText.caption.copyWith(
                color: Colors.amber,
                fontSize: 13,
              ),
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
      padding: const EdgeInsets.symmetric(vertical: XActSpace.s1),
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
              color: Colors.white.withValues(alpha: .04),
              borderRadius: const BorderRadius.all(Radius.circular(8)),
              border: Border.all(color: Colors.white.withValues(alpha: .06)),
            ),
          ),
        ),
        child: PlayerTileContent(player: player, dotColor: dotColor),
      ),
    );
  }
}
