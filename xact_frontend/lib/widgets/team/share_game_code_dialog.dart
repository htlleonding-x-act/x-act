import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:share_plus/share_plus.dart';

import '../xact_branding.dart';

class ShareGameCodeDialog extends StatefulWidget {
  final String gameCode;
  final String gameName;
  final bool autoShare;

  const ShareGameCodeDialog({
    super.key,
    required this.gameCode,
    required this.gameName,
    this.autoShare = false,
  });

  static Future<void> show(
    BuildContext context, {
    required String gameCode,
    required String gameName,
    bool autoShare = false,
  }) {
    return showDialog<void>(
      context: context,
      builder: (_) => ShareGameCodeDialog(
        gameCode: gameCode,
        gameName: gameName,
        autoShare: autoShare,
      ),
    );
  }

  @override
  State<ShareGameCodeDialog> createState() => _ShareGameCodeDialogState();
}

class _ShareGameCodeDialogState extends State<ShareGameCodeDialog> {
  @override
  void initState() {
    super.initState();
    if (widget.autoShare) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _share(context);
      });
    }
  }

  String get _shareMessage =>
      'Join my X-ACT game "${widget.gameName}"! Game code: ${widget.gameCode}';

  Future<void> _copy(BuildContext context) async {
    await Clipboard.setData(ClipboardData(text: widget.gameCode));
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
      sharePositionOrigin:
          box != null ? box.localToGlobal(Offset.zero) & box.size : null,
    );
    await SharePlus.instance.share(params);
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: XActColors.surface,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(24),
        side: BorderSide(color: XActColors.hairlineSoft),
      ),
      insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      XActBranding.buildEyebrow('Invite players'),
                      const SizedBox(height: 2),
                      Text('Share Game Code', style: XActText.heading),
                    ],
                  ),
                ),
                XActBranding.circleIconButton(
                  icon: Icons.close_rounded,
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const SizedBox(height: 14),
            Center(
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(16),
                  boxShadow: XActElevation.e2,
                ),
                child: QrImageView(
                  data: widget.gameCode,
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
            const SizedBox(height: 18),
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 16,
                vertical: 14,
              ),
              decoration: BoxDecoration(
                color: XActColors.bg2,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: XActColors.hairlineSoft),
              ),
              child: Row(
                children: [
                  XActBranding.buildEyebrow('Game code'),
                  const Spacer(),
                  Text(
                    widget.gameCode.split('').join(' '),
                    style: XActText.codeXl.copyWith(fontSize: 22),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: XActBranding.buildGhostButton(
                    text: 'Copy',
                    icon: Icons.copy_rounded,
                    height: 48,
                    onPressed: () => _copy(context),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: XActBranding.buildSecondaryButton(
                    text: 'Share',
                    icon: Icons.share_rounded,
                    height: 48,
                    onPressed: () => _share(context),
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
