import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Header row for the game lobby showing title and player count.
class GameLobbyHeader extends StatelessWidget {
  final String gameName;
  final int totalPlayers;
  final bool isLeader;
  final VoidCallback? onClose;

  const GameLobbyHeader({
    super.key,
    required this.gameName,
    required this.totalPlayers,
    required this.isLeader,
    this.onClose,
  });

  @override
  Widget build(BuildContext context) {
    final subtitle =
        '$totalPlayers ${totalPlayers == 1 ? 'player' : 'players'}'
        '${isLeader ? ' · Host' : ''}';

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      child: Row(
        children: [
          XActBranding.circleIconButton(
            icon: Icons.arrow_back_rounded,
            onPressed: () => Navigator.of(context).maybePop(),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                XActBranding.buildEyebrow('Lobby · $subtitle'),
                const SizedBox(height: 2),
                Text(
                  gameName,
                  style: XActText.title.copyWith(fontSize: 24),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          if (onClose != null)
            XActBranding.circleIconButton(
              icon: Icons.close_rounded,
              onPressed: onClose!,
            ),
        ],
      ),
    );
  }
}
