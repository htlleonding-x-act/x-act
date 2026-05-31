import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// Game-code hero card with copy / QR / share actions.
class GameCodeCard extends StatelessWidget {
  final String gameCode;
  final String codeLabel;
  final VoidCallback onCopy;
  final VoidCallback onShare;
  final VoidCallback? onShowQr;

  const GameCodeCard({
    super.key,
    required this.gameCode,
    this.codeLabel = 'Game code',
    required this.onCopy,
    required this.onShare,
    this.onShowQr,
  });

  @override
  Widget build(BuildContext context) {
    final formatted = gameCode.split('').join(' ');
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e2,
      ),
      child: Stack(
        clipBehavior: Clip.hardEdge,
        children: [
          Positioned(
            top: -40,
            right: -30,
            child: Container(
              width: 160,
              height: 160,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: RadialGradient(
                  colors: [
                    XActColors.primary.withValues(alpha: .35),
                    XActColors.primary.withValues(alpha: 0),
                  ],
                ),
              ),
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              XActBranding.buildEyebrow(codeLabel),
              const SizedBox(height: 4),
              Text(
                formatted,
                style: XActText.codeXl.copyWith(
                  fontSize: 24,
                  letterSpacing: 3.4,
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: XActBranding.buildGhostButton(
                      text: 'Copy',
                      icon: Icons.copy_rounded,
                      height: 42,
                      onPressed: onCopy,
                    ),
                  ),
                  if (onShowQr != null) ...[
                    const SizedBox(width: 8),
                    Expanded(
                      child: XActBranding.buildGhostButton(
                        text: 'QR',
                        icon: Icons.qr_code_rounded,
                        height: 42,
                        onPressed: onShowQr,
                      ),
                    ),
                  ],
                  const SizedBox(width: 8),
                  Expanded(
                    child: XActBranding.buildSecondaryButton(
                      text: 'Share',
                      icon: Icons.share_rounded,
                      height: 42,
                      onPressed: onShare,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}
