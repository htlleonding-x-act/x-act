import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:share_plus/share_plus.dart';

import '../xact_branding.dart';

class ShareGameCodeDialog extends StatelessWidget {
  final String gameCode;
  final String gameName;

  const ShareGameCodeDialog({
    super.key,
    required this.gameCode,
    required this.gameName,
  });

  static Future<void> show(
    BuildContext context, {
    required String gameCode,
    required String gameName,
  }) {
    return showDialog<void>(
      context: context,
      builder: (_) =>
          ShareGameCodeDialog(gameCode: gameCode, gameName: gameName),
    );
  }

  String get _shareMessage =>
      'Join my X-ACT game "$gameName"! Game code: $gameCode';

  Future<void> _copy(BuildContext context) async {
    await Clipboard.setData(ClipboardData(text: gameCode));
    if (!context.mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Game code copied!')));
  }

  Future<void> _share(BuildContext context) async {
    final box = context.findRenderObject() as RenderBox?;
    final params = ShareParams(
      text: _shareMessage,
      subject: 'X-ACT game code',
      sharePositionOrigin: box != null
          ? box.localToGlobal(Offset.zero) & box.size
          : null,
    );
    await SharePlus.instance.share(params);
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: XActBranding.cardColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const Expanded(
                  child: Text(
                    'Share Game Code',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, color: Colors.white70),
                  onPressed: () => Navigator.of(context).pop(),
                  tooltip: 'Close',
                ),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Scan the QR code or share the code below to invite players.',
              style: const TextStyle(color: Colors.white60, fontSize: 13),
            ),
            const SizedBox(height: 20),
            Center(
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: QrImageView(
                  data: gameCode,
                  version: QrVersions.auto,
                  size: 200,
                  backgroundColor: Colors.white,
                  eyeStyle: const QrEyeStyle(
                    eyeShape: QrEyeShape.square,
                    color: Colors.black,
                  ),
                  dataModuleStyle: const QrDataModuleStyle(
                    dataModuleShape: QrDataModuleShape.square,
                    color: Colors.black,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 16,
                vertical: 12,
              ),
              decoration: BoxDecoration(
                color: XActBranding.backgroundColor,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Row(
                children: [
                  const Expanded(
                    child: Text(
                      'Game Code',
                      style: TextStyle(color: Colors.white54, fontSize: 12),
                    ),
                  ),
                  Text(
                    gameCode,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 4,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () => _copy(context),
                    icon: const Icon(Icons.copy, size: 18),
                    label: const Text('Copy'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.white12,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () => _share(context),
                    icon: const Icon(Icons.share, size: 18),
                    label: const Text('Share'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: XActBranding.primaryBlue,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
