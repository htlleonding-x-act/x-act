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
    final initial = player.name.trim().isEmpty
        ? '?'
        : player.name.trim()[0].toUpperCase();
    final isYou = player.isCurrentUser;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: XActColors.bg2,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.drag_indicator, color: XActColors.text5, size: 18),
          const SizedBox(width: 6),
          Container(
            width: 30,
            height: 30,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  dotColor,
                  dotColor.withValues(alpha: .6),
                ],
              ),
              boxShadow: [
                BoxShadow(
                  color: dotColor.withValues(alpha: .35),
                  blurRadius: 8,
                ),
              ],
            ),
            alignment: Alignment.center,
            child: Text(
              initial,
              style: XActText.bodySm.copyWith(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: Colors.white,
              ),
            ),
          ),
          const SizedBox(width: 10),
          Text(
            player.name,
            style: XActText.bodySm.copyWith(fontWeight: FontWeight.w500),
          ),
          if (player.isTeamLeader) ...[
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: .06),
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                'HOST',
                style: XActText.eyebrow.copyWith(
                  fontSize: 10,
                  color: XActColors.text3,
                ),
              ),
            ),
          ],
          if (isYou) ...[
            const SizedBox(width: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color: XActColors.warning.withValues(alpha: .16),
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                'YOU',
                style: XActText.eyebrow.copyWith(
                  fontSize: 10,
                  color: XActColors.warning,
                ),
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
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: LongPressDraggable<LobbyPlayer>(
        data: player,
        delay: const Duration(milliseconds: 150),
        feedback: Material(
          color: Colors.transparent,
          child: Opacity(
            opacity: 0.9,
            child: SizedBox(
              width: MediaQuery.of(context).size.width - 64,
              child: PlayerTileContent(player: player, dotColor: dotColor),
            ),
          ),
        ),
        childWhenDragging: Padding(
          padding: const EdgeInsets.symmetric(vertical: 2),
          child: Container(
            height: 42,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: .04),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: XActColors.hairlineSoft),
            ),
          ),
        ),
        child: PlayerTileContent(player: player, dotColor: dotColor),
      ),
    );
  }
}
