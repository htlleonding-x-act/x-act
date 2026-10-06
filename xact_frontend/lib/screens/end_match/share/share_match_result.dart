import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:share_plus/share_plus.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../winner_hero.dart';
import 'match_share_card.dart';

Future<void> showMatchShareSheet(BuildContext context, GameResults results) {
  return showDialog<void>(
    context: context,
    builder: (_) => _MatchShareDialog(results: results),
  );
}

class _MatchShareDialog extends StatefulWidget {
  const _MatchShareDialog({required this.results});

  final GameResults results;

  @override
  State<_MatchShareDialog> createState() => _MatchShareDialogState();
}

class _MatchShareDialogState extends State<_MatchShareDialog> {
  // sharper than the screen, so the image still looks good when zoomed in
  static const double _pixelRatio = 3;

  final GlobalKey _cardKey = GlobalKey();
  bool _sharing = false;

  Future<void> _share(BuildContext buttonContext) async {
    final box = buttonContext.findRenderObject() as RenderBox?;
    final origin = box == null
        ? null
        : box.localToGlobal(Offset.zero) & box.size;
    final messenger = ScaffoldMessenger.of(context);
    setState(() => _sharing = true);

    try {
      // an image captured before the fonts arrived would show fallback glyphs.
      // a font that fails to load must not stop the share, the fallback is fine
      try {
        await GoogleFonts.pendingFonts();
      } catch (_) {}
      await WidgetsBinding.instance.endOfFrame;

      final boundary =
          _cardKey.currentContext!.findRenderObject()! as RenderRepaintBoundary;
      final image = await boundary.toImage(pixelRatio: _pixelRatio);
      final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
      image.dispose();

      final fileName = 'xact-match-${widget.results.sessionId}.png';
      final hero = HeroContent.of(widget.results);
      await SharePlus.instance.share(
        ShareParams(
          files: [
            XFile.fromData(
              bytes!.buffer.asUint8List(),
              mimeType: 'image/png',
              name: fileName,
            ),
          ],
          fileNameOverrides: [fileName],
          text:
              'X-ACT "${widget.results.sessionName}": ${hero.eyebrow} – ${hero.headline}',
          sharePositionOrigin: origin,
          downloadFallbackEnabled: true,
        ),
      );
    } catch (error) {
      messenger.showSnackBar(
        const SnackBar(content: Text('Could not share the image.')),
      );
    } finally {
      if (mounted) {
        setState(() => _sharing = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: XActColors.surface,
      surfaceTintColor: Colors.transparent,
      insetPadding: const EdgeInsets.all(XActSpace.s4),
      shape: RoundedRectangleBorder(
        borderRadius: XActRadius.xl,
        side: BorderSide(color: XActColors.hairlineSoft),
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: MatchShareCard.size.width + 2 * XActSpace.s4,
        ),
        child: Padding(
          padding: const EdgeInsets.all(XActSpace.s4),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Flexible(
                child: FittedBox(
                  child: ClipRRect(
                    borderRadius: XActRadius.md,
                    child: RepaintBoundary(
                      key: _cardKey,
                      child: MatchShareCard(results: widget.results),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: XActSpace.s4),
              Builder(
                builder: (buttonContext) => XActBranding.buildSecondaryButton(
                  text: _sharing ? 'Preparing…' : 'Share image',
                  icon: Icons.ios_share_rounded,
                  onPressed: _sharing ? null : () => _share(buttonContext),
                  height: 48,
                ),
              ),
              const SizedBox(height: XActSpace.s2),
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Close'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
