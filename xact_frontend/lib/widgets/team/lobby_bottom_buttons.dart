import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Bottom action buttons: Randomize Teams + Start Game.
class LobbyBottomButtons extends StatelessWidget {
  final bool canStartGame;
  final VoidCallback onRandomize;
  final VoidCallback onStartGame;

  const LobbyBottomButtons({
    super.key,
    required this.canStartGame,
    required this.onRandomize,
    required this.onStartGame,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 12),
      child: Column(
        children: [
          // ── Randomize Teams ───────────────────────────────────────────
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: onRandomize,
              icon: const Icon(Icons.shuffle, size: 18),
              label: const Text('Randomize Teams'),
              style: ElevatedButton.styleFrom(
                backgroundColor: XActBranding.primaryBlue,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ),
          const SizedBox(height: 8),

          // ── Start Game ────────────────────────────────────────────────
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: canStartGame ? onStartGame : null,
              icon: const Icon(Icons.play_arrow, size: 20),
              label: const Text(
                'Start Game',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: canStartGame ? Colors.green : Colors.white10,
                foregroundColor: canStartGame ? Colors.white : Colors.white38,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ),
          if (!canStartGame) ...[
            const SizedBox(height: 6),
            const Text(
              'Need at least 1 Mister X and 1 Detective to start',
              style: TextStyle(color: Colors.white38, fontSize: 12),
            ),
          ],
        ],
      ),
    );
  }
}
