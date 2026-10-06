import 'package:flutter/material.dart';

import '../../api/game_results.dart';
import '../../widgets/xact_branding.dart';
import 'end_match_format.dart';
import 'hero_burst_painter.dart';

/// what the header says about how the match ended
final class HeroContent {
  const HeroContent({
    required this.eyebrow,
    required this.headline,
    required this.subtitle,
    required this.icon,
    required this.accent,
  });

  final String eyebrow;
  final String headline;
  final String subtitle;
  final IconData icon;
  final Color accent;

  factory HeroContent.of(GameResults results) {
    final winner = results.winnerTeam;
    final catches = results.catches;

    if (winner == null) {
      final abandoned = results.endReason == GameEndReason.abandoned;
      return HeroContent(
        eyebrow: 'Result',
        headline: abandoned ? 'Match abandoned' : 'No winner',
        subtitle: abandoned
            ? 'Everyone left before the match was decided.'
            : 'Nobody held the Mister X role when the match ended.',
        icon: Icons.flag_rounded,
        accent: XActColors.roleSpectator,
      );
    }

    if (catches.isEmpty) {
      return HeroContent(
        eyebrow: 'Mister X escaped',
        headline: winner.teamName,
        subtitle:
            'Stayed hidden for the whole ${formatShortDuration(results.duration)}.',
        icon: Icons.visibility_off_rounded,
        accent: XActColors.roleMrX,
      );
    }

    final lastCatch = catches.last;
    return HeroContent(
      eyebrow: 'Winner',
      headline: winner.teamName,
      subtitle: lastCatch.teamId == winner.teamId
          ? 'Caught Mister X at ${formatMatchClock(lastCatch.offsetSeconds)} and held on to the end.'
          : 'Held the Mister X role when the match ended.',
      icon: Icons.emoji_events_rounded,
      accent: XActColors.warning,
    );
  }
}

class WinnerHero extends StatefulWidget {
  const WinnerHero({
    super.key,
    required this.results,
    required this.isWinner,
    this.compact = false,
    this.onShare,
  });

  final GameResults results;
  final bool isWinner;

  /// a single line so the map and leaderboard get the room
  final bool compact;
  final VoidCallback? onShare;

  @override
  State<WinnerHero> createState() => _WinnerHeroState();
}

class _WinnerHeroState extends State<WinnerHero>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1600),
  );

  late final Animation<double> _iconScale = CurvedAnimation(
    parent: _controller,
    curve: const Interval(0, .45, curve: Curves.elasticOut),
  );
  late final Animation<double> _burst = CurvedAnimation(
    parent: _controller,
    curve: const Interval(0, .7),
  );
  late final Animation<double> _eyebrowFade = CurvedAnimation(
    parent: _controller,
    curve: const Interval(.2, .6, curve: Curves.easeOut),
  );
  late final Animation<double> _headlineFade = CurvedAnimation(
    parent: _controller,
    curve: const Interval(.35, .8, curve: Curves.easeOut),
  );
  late final Animation<Offset> _headlineSlide = Tween(
    begin: const Offset(0, .3),
    end: Offset.zero,
  ).animate(_headlineFade);
  late final Animation<double> _subtitleFade = CurvedAnimation(
    parent: _controller,
    curve: const Interval(.6, 1, curve: Curves.easeOut),
  );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_controller.status == AnimationStatus.dismissed) {
      if (MediaQuery.disableAnimationsOf(context)) {
        _controller.value = 1;
      } else {
        _controller.forward();
      }
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final content = HeroContent.of(widget.results);

    return AnimatedSize(
      duration: const Duration(milliseconds: 250),
      curve: Curves.easeOutCubic,
      alignment: Alignment.topCenter,
      child: Container(
        width: double.infinity,
        padding: EdgeInsets.all(widget.compact ? XActSpace.s3 : XActSpace.s5),
        decoration: BoxDecoration(
          borderRadius: XActRadius.lg,
          border: Border.all(color: content.accent.withValues(alpha: .35)),
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [
              content.accent.withValues(alpha: .18),
              XActColors.surface,
            ],
          ),
          boxShadow: XActElevation.e2,
        ),
        child: widget.compact
            ? _buildCompact(content)
            : _buildFull(content),
      ),
    );
  }

  Widget _buildCompact(HeroContent content) {
    return Row(
      children: [
        _iconTile(content, size: 40, iconSize: 22),
        const SizedBox(width: XActSpace.s3),
        Expanded(
          child: Text(
            '${content.eyebrow}: ${content.headline}',
            style: XActText.subheading,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
        if (widget.isWinner) _youWonPill(content),
        if (widget.onShare != null) _shareButton(),
      ],
    );
  }

  Widget _buildFull(HeroContent content) {
    final endReason = widget.results.endReason;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 96,
          height: 96,
          child: AnimatedBuilder(
            animation: _controller,
            builder: (context, child) => CustomPaint(
              painter: HeroBurstPainter(
                progress: _burst.value,
                color: content.accent,
                seed: widget.results.sessionId,
              ),
              child: child,
            ),
            child: Center(
              child: ScaleTransition(
                scale: _iconScale,
                child: _iconTile(content, size: 64, iconSize: 34),
              ),
            ),
          ),
        ),
        const SizedBox(width: XActSpace.s3),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: FadeTransition(
                      opacity: _eyebrowFade,
                      child: XActBranding.buildEyebrow(
                        content.eyebrow,
                        color: content.accent,
                      ),
                    ),
                  ),
                  if (widget.onShare != null) _shareButton(),
                ],
              ),
              const SizedBox(height: XActSpace.s1),
              FadeTransition(
                opacity: _headlineFade,
                child: SlideTransition(
                  position: _headlineSlide,
                  child: Text(
                    content.headline,
                    style: XActText.displaySm.copyWith(fontSize: 28),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ),
              const SizedBox(height: XActSpace.s1),
              FadeTransition(
                opacity: _subtitleFade,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      content.subtitle,
                      style: XActText.bodySm.copyWith(color: XActColors.text2),
                    ),
                    if (widget.isWinner || endReason != null) ...[
                      const SizedBox(height: XActSpace.s2),
                      Wrap(
                        spacing: XActSpace.s2,
                        runSpacing: XActSpace.s2,
                        children: [
                          if (widget.isWinner) _youWonPill(content),
                          if (endReason != null)
                            _chip(endReasonLabel(endReason), XActColors.text3),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _iconTile(
    HeroContent content, {
    required double size,
    required double iconSize,
  }) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: content.accent.withValues(alpha: .18),
        borderRadius: XActRadius.md,
        border: Border.all(color: content.accent.withValues(alpha: .5)),
      ),
      child: Icon(content.icon, color: content.accent, size: iconSize),
    );
  }

  Widget _youWonPill(HeroContent content) => Padding(
    padding: const EdgeInsets.only(left: XActSpace.s1),
    child: _chip('You won', content.accent, filled: true),
  );

  Widget _chip(String label, Color color, {bool filled = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: XActSpace.s2,
        vertical: 3,
      ),
      decoration: BoxDecoration(
        color: filled ? color.withValues(alpha: .2) : Colors.transparent,
        borderRadius: XActRadius.pill,
        border: Border.all(color: color.withValues(alpha: .6)),
      ),
      child: Text(
        label,
        style: XActText.caption.copyWith(
          color: filled ? color : XActColors.text2,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _shareButton() {
    return IconButton(
      tooltip: 'Share result',
      visualDensity: VisualDensity.compact,
      onPressed: widget.onShare,
      icon: Icon(Icons.ios_share_rounded, color: XActColors.text2, size: 20),
    );
  }
}
