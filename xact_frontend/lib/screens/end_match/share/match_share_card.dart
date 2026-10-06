import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';
import '../overview/award_style.dart';
import '../winner_hero.dart';
import 'route_sketch_painter.dart';

/// the result as a fixed size card, rendered to an image for sharing
class MatchShareCard extends StatelessWidget {
  const MatchShareCard({super.key, required this.results});

  final GameResults results;

  static const Size size = Size(360, 520);
  static const int _maxAwards = 3;

  @override
  Widget build(BuildContext context) {
    final hero = HeroContent.of(results);
    final totalDistance = results.players.fold<double>(
      0,
      (sum, m) => sum + m.stats.distanceMeters,
    );

    return Container(
      width: size.width,
      height: size.height,
      padding: const EdgeInsets.all(XActSpace.s5),
      decoration: BoxDecoration(
        color: XActColors.bg,
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            hero.accent.withValues(alpha: .28),
            XActColors.bg,
            XActColors.bg2,
          ],
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  results.sessionName,
                  style: XActText.caption.copyWith(color: XActColors.text2),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: XActSpace.s2),
              Text(
                formatDateTime(results.startTime).split(' ').first,
                style: XActText.caption,
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s3),
          Row(
            children: [
              Icon(hero.icon, color: hero.accent, size: 28),
              const SizedBox(width: XActSpace.s2),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    XActBranding.buildEyebrow(hero.eyebrow, color: hero.accent),
                    Text(
                      hero.headline,
                      style: XActText.displaySm.copyWith(fontSize: 24),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: XActSpace.s3),
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                color: XActColors.surface.withValues(alpha: .7),
                borderRadius: XActRadius.md,
                border: Border.all(color: XActColors.hairlineSoft),
              ),
              child: CustomPaint(
                painter: RouteSketchPainter(results),
                size: Size.infinite,
              ),
            ),
          ),
          const SizedBox(height: XActSpace.s3),
          Row(
            children: [
              _stat('Duration', formatShortDuration(results.duration)),
              _stat('Distance', formatDistance(totalDistance)),
              _stat('Catches', '${results.catches.length}'),
            ],
          ),
          const SizedBox(height: XActSpace.s3),
          for (final award in results.awards.take(_maxAwards)) _award(award),
          const SizedBox(height: XActSpace.s2),
          Align(
            alignment: Alignment.centerRight,
            child: Text('Played on X-ACT', style: XActText.eyebrow),
          ),
        ],
      ),
    );
  }

  Widget _stat(String label, String value) {
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: XActText.caption),
          Text(value, style: XActText.subheading),
        ],
      ),
    );
  }

  Widget _award(MatchAward award) {
    final style = AwardStyle.of(award.type!);
    final names = award.memberIds
        .map((id) => results.memberById(id)?.displayName ?? 'Unknown')
        .join(', ');

    return Padding(
      padding: const EdgeInsets.only(bottom: 3),
      child: Row(
        children: [
          Icon(style.icon, size: 14, color: style.color),
          const SizedBox(width: XActSpace.s2),
          Text(
            style.title,
            style: XActText.caption.copyWith(
              color: style.color,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(width: XActSpace.s2),
          Expanded(
            child: Text(
              names,
              style: XActText.caption.copyWith(color: XActColors.text1),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }
}
