import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import 'award_style.dart';

class AwardCard extends StatelessWidget {
  const AwardCard({
    super.key,
    required this.award,
    required this.winnerNames,
    required this.entrance,
    this.onShowOnReplay,
  });

  final MatchAward award;
  final List<String> winnerNames;

  /// 0 to 1, drives the staggered fade and slide in
  final Animation<double> entrance;
  final VoidCallback? onShowOnReplay;

  @override
  Widget build(BuildContext context) {
    final style = AwardStyle.of(award.type!);

    return FadeTransition(
      opacity: entrance,
      child: SlideTransition(
        position: Tween(
          begin: const Offset(0, .25),
          end: Offset.zero,
        ).animate(entrance),
        child: Container(
          padding: const EdgeInsets.all(XActSpace.s4),
          decoration: BoxDecoration(
            color: XActColors.surface,
            borderRadius: XActRadius.lg,
            border: Border.all(color: style.color.withValues(alpha: .35)),
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [style.color.withValues(alpha: .14), XActColors.surface],
            ),
          ),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: style.color.withValues(alpha: .18),
                  border: Border.all(color: style.color.withValues(alpha: .6)),
                ),
                child: Icon(style.icon, color: style.color),
              ),
              const SizedBox(width: XActSpace.s3),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    XActBranding.buildEyebrow(style.title, color: style.color),
                    const SizedBox(height: 2),
                    Text(
                      winnerNames.join(', '),
                      style: XActText.subheading,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      '${style.description} · ${style.formatValue(award.value)}',
                      style: XActText.caption,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
              if (onShowOnReplay != null)
                IconButton(
                  tooltip: 'Show on replay',
                  onPressed: onShowOnReplay,
                  icon: Icon(
                    Icons.play_circle_outline_rounded,
                    color: XActColors.text2,
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
