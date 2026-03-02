import 'package:flutter/material.dart';

/// Header row for the game lobby showing title, player count and QR button.
class LobbyHeader extends StatelessWidget {
  final int totalPlayers;
  final bool isLeader;
  final VoidCallback? onQrPressed;

  const LobbyHeader({
    super.key,
    required this.totalPlayers,
    required this.isLeader,
    this.onQrPressed,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Game Lobby',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  '$totalPlayers players in lobby'
                  '${isLeader ? '  •  Lobby Leader' : ''}',
                  style: const TextStyle(color: Colors.white60, fontSize: 13),
                ),
              ],
            ),
          ),
          IconButton(
            icon: const Icon(Icons.qr_code, color: Colors.white70),
            onPressed: onQrPressed,
          ),
        ],
      ),
    );
  }
}
