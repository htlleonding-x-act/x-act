import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Card displaying the lobby code with copy and share buttons.
class GameCodeCard extends StatelessWidget {
  final String gameCode;
  final String codeLabel;
  final VoidCallback onCopy;
  final VoidCallback onShare;

  const GameCodeCard({
    super.key,
    required this.gameCode,
    this.codeLabel = 'Game Code',
    required this.onCopy,
    required this.onShare,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(
        horizontal: XActSpace.s4,
        vertical: XActSpace.s3,
      ),
      decoration: const BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.md,
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  codeLabel,
                  style: XActText.caption.copyWith(color: XActColors.text3),
                ),
                const SizedBox(height: XActSpace.s1),
                Text(
                  gameCode,
                  style: XActText.title.copyWith(
                    fontFamilyFallback: const ['monospace'],
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            onPressed: onCopy,
            icon: const Icon(Icons.copy, size: 18),
            color: XActColors.text1,
            tooltip: 'Copy code',
            style: IconButton.styleFrom(
              backgroundColor: Colors.white.withValues(alpha: .08),
              shape: const RoundedRectangleBorder(borderRadius: XActRadius.sm),
            ),
          ),
          const SizedBox(width: XActSpace.s2),
          ElevatedButton.icon(
            onPressed: onShare,
            icon: const Icon(Icons.share, size: 16),
            label: const Text('Share'),
            style: ElevatedButton.styleFrom(
              backgroundColor: XActColors.secondary,
              foregroundColor: XActColors.text1,
              shape: const RoundedRectangleBorder(borderRadius: XActRadius.sm),
            ),
          ),
        ],
      ),
    );
  }
}
