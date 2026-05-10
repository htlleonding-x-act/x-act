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
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s1,
        XActSpace.s4,
        XActSpace.s3,
      ),
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
                backgroundColor: XActColors.secondary,
                foregroundColor: XActColors.text1,
                shape: const RoundedRectangleBorder(
                  borderRadius: XActRadius.md,
                ),
                padding: const EdgeInsets.symmetric(vertical: XActSpace.s3),
              ),
            ),
          ),
          const SizedBox(height: XActSpace.s2),

          // ── Start Game ────────────────────────────────────────────────
          XActBranding.buildSuccessButton(
            text: 'Start Game',
            icon: Icons.play_arrow,
            onPressed: canStartGame ? onStartGame : null,
          ),
          if (!canStartGame) ...[
            const SizedBox(height: XActSpace.s1),
            Text(
              'Need 1 Mister X, 1 Detective, and no players in Unassigned',
              style: XActText.caption.copyWith(color: XActColors.text4),
            ),
          ],
        ],
      ),
    );
  }
}
