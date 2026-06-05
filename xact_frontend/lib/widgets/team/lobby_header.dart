import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Header row for the game lobby showing title and player count.
class GameLobbyHeader extends StatelessWidget {
  final String gameName;
  final int totalPlayers;
  final bool isLeader;
  final VoidCallback? onClose;
  final VoidCallback? onViewMap;
  final VoidCallback? onSettings;
  final VoidCallback? onProfile;

  const GameLobbyHeader({
    super.key,
    required this.gameName,
    required this.totalPlayers,
    required this.isLeader,
    this.onClose,
    this.onViewMap,
    this.onSettings,
    this.onProfile,
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
          if (onViewMap != null) ...[
            const SizedBox(width: 4),
            XActBranding.circleIconButton(
              icon: Icons.map_outlined,
              onPressed: onViewMap!,
            ),
          ],
          if (onSettings != null) ...[
            const SizedBox(width: 4),
            XActBranding.circleIconButton(
              icon: Icons.settings_rounded,
              onPressed: onSettings!,
            ),
          ],
          if (onProfile != null) ...[
            const SizedBox(width: 4),
            XActBranding.circleIconButton(
              icon: Icons.person_outline_rounded,
              onPressed: onProfile!,
            ),
          ],
          if (onClose != null) ...[
            const SizedBox(width: 4),
            XActBranding.circleIconButton(
              icon: Icons.close_rounded,
              onPressed: onClose!,
            ),
          ],
        ],
      ),
    );
  }
}
