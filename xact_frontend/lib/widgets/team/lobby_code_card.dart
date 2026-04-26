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
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  codeLabel,
                  style: const TextStyle(color: Colors.white54, fontSize: 12),
                ),
                const SizedBox(height: 4),
                Text(
                  gameCode,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 4,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            onPressed: onCopy,
            icon: const Icon(Icons.copy, size: 18),
            color: Colors.white,
            tooltip: 'Copy code',
            style: IconButton.styleFrom(
              backgroundColor: Colors.white12,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
          ),
          const SizedBox(width: 8),
          ElevatedButton.icon(
            onPressed: onShare,
            icon: const Icon(Icons.share, size: 16),
            label: const Text('Share'),
            style: ElevatedButton.styleFrom(
              backgroundColor: XActBranding.primaryBlue,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
