import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Card displaying the lobby code with a copy button.
class LobbyCodeCard extends StatelessWidget {
  final String lobbyCode;
  final String codeLabel;
  final VoidCallback onCopy;

  const LobbyCodeCard({
    super.key,
    required this.lobbyCode,
    this.codeLabel = 'Game Code',
    required this.onCopy,
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
                  style: TextStyle(color: Colors.white54, fontSize: 12),
                ),
                const SizedBox(height: 4),
                Text(
                  lobbyCode,
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
          ElevatedButton.icon(
            onPressed: onCopy,
            icon: const Icon(Icons.copy, size: 16),
            label: const Text('Copy'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.white12,
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
