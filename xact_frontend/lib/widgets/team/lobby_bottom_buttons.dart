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
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 28),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [Color(0x00000000), XActColors.bg],
          stops: [0, .4],
        ),
      ),
      child: Column(
        children: [
          XActBranding.buildGhostButton(
            text: 'Randomize Teams',
            icon: Icons.shuffle_rounded,
            height: 48,
            onPressed: onRandomize,
          ),
          const SizedBox(height: XActSpace.s3),
          XActBranding.buildSuccessButton(
            text: 'Start Game',
            icon: Icons.play_arrow_rounded,
            onPressed: canStartGame ? onStartGame : null,
          ),
          if (!canStartGame) ...[
            const SizedBox(height: 6),
            Text(
              'Need 1 Mister X, 1 Detective, and no players in Unassigned',
              style: XActText.caption.copyWith(color: XActColors.text4),
              textAlign: TextAlign.center,
            ),
          ],
        ],
      ),
    );
  }
}
