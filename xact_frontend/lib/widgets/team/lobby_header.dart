import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Header row for the game lobby showing title, player count and QR button.
class GameLobbyHeader extends StatelessWidget {
  final String gameName;
  final int totalPlayers;
  final bool isLeader;
  final VoidCallback? onQrPressed;

  const GameLobbyHeader({
    super.key,
    required this.gameName,
    required this.totalPlayers,
    required this.isLeader,
    this.onQrPressed,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s3,
        XActSpace.s4,
        XActSpace.s1,
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  gameName,
                  style: XActText.heading.copyWith(fontSize: 22),
                ),
                const SizedBox(height: 2),
                Text(
                  '$totalPlayers players in game lobby'
                  '${isLeader ? '  •  Game Host' : ''}',
                  style: XActText.caption.copyWith(
                    color: XActColors.text2,
                    fontSize: 13,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            icon: Icon(Icons.qr_code, color: XActColors.text2),
            onPressed: onQrPressed,
            tooltip: 'Share game code',
          ),
        ],
      ),
    );
  }
}
