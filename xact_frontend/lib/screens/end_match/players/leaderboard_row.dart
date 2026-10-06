import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';
import 'transport_split_bar.dart';

class LeaderboardRow extends StatefulWidget {
  const LeaderboardRow({
    super.key,
    required this.rank,
    required this.member,
    required this.team,
    required this.accent,
    required this.metricValue,
    required this.isMe,
    this.onWatchRoute,
  });

  final int rank;
  final ResultMember member;
  final ResultTeam? team;
  final Color accent;
  final String metricValue;
  final bool isMe;
  final VoidCallback? onWatchRoute;

  @override
  State<LeaderboardRow> createState() => _LeaderboardRowState();
}

class _LeaderboardRowState extends State<LeaderboardRow> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final accent = widget.accent;
    final stats = widget.member.stats;

    return Container(
      decoration: BoxDecoration(
        color: widget.isMe ? XActColors.surface2 : XActColors.surface,
        borderRadius: XActRadius.md,
        border: Border.all(
          color: widget.isMe ? accent.withValues(alpha: .6) : XActColors.hairlineSoft,
        ),
      ),
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          borderRadius: XActRadius.md,
          onTap: () => setState(() => _expanded = !_expanded),
          child: AnimatedSize(
            duration: const Duration(milliseconds: 200),
            curve: Curves.easeOutCubic,
            alignment: Alignment.topCenter,
            child: Padding(
              padding: const EdgeInsets.all(XActSpace.s3),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildHeader(accent),
                  if (_expanded) ...[
                    const SizedBox(height: XActSpace.s3),
                    Wrap(
                      spacing: XActSpace.s5,
                      runSpacing: XActSpace.s2,
                      children: [
                        _metric('Distance', formatDistance(stats.distanceMeters)),
                        _metric('Top speed', formatSpeed(stats.topSpeedKmh)),
                        _metric('Pace', formatPace(stats.avgPaceSecondsPerKm)),
                        _metric('Moving', formatShortDuration(stats.movingTime)),
                        _metric('As Mister X', formatShortDuration(stats.mrXTime)),
                        _metric('Catches', '${stats.catchesMade}'),
                        _metric('Power-ups', '${stats.powerUpsUsed}'),
                        _metric(
                          'Out of area',
                          stats.outOfBoundsCount == 0
                              ? '–'
                              : '${formatShortDuration(stats.outOfBounds)} (${stats.outOfBoundsCount}×)',
                        ),
                      ],
                    ),
                    const SizedBox(height: XActSpace.s3),
                    TransportSplitBar(stats: stats),
                    if (widget.onWatchRoute != null && widget.member.route.isNotEmpty) ...[
                      const SizedBox(height: XActSpace.s2),
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton.icon(
                          onPressed: widget.onWatchRoute,
                          icon: const Icon(Icons.play_arrow_rounded),
                          label: const Text('Watch route'),
                        ),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildHeader(Color accent) {
    final medal = switch (widget.rank) {
      1 => const Color(0xFFFACC15),
      2 => const Color(0xFFCBD5E1),
      3 => const Color(0xFFD97706),
      _ => null,
    };

    return Row(
      children: [
        SizedBox(
          width: 28,
          child: Text(
            '${widget.rank}',
            textAlign: TextAlign.center,
            style: XActText.mono.copyWith(
              fontSize: 16,
              color: medal ?? XActColors.text3,
            ),
          ),
        ),
        const SizedBox(width: XActSpace.s2),
        CircleAvatar(
          radius: 16,
          backgroundColor: accent.withValues(alpha: .25),
          child: Text(
            initialOf(widget.member.displayName),
            style: XActText.bodySm.copyWith(color: accent, fontWeight: FontWeight.w700),
          ),
        ),
        const SizedBox(width: XActSpace.s3),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                widget.isMe ? '${widget.member.displayName} (you)' : widget.member.displayName,
                style: XActText.bodySm.copyWith(fontWeight: FontWeight.w700),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                widget.team?.teamName ?? 'No team',
                style: XActText.caption,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
        Text(
          widget.metricValue,
          style: XActText.bodySm.copyWith(fontWeight: FontWeight.w700, color: accent),
        ),
        Icon(
          _expanded ? Icons.expand_less_rounded : Icons.expand_more_rounded,
          color: XActColors.text4,
        ),
      ],
    );
  }

  Widget _metric(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, style: XActText.caption),
        Text(value, style: XActText.bodySm.copyWith(fontWeight: FontWeight.w600)),
      ],
    );
  }
}
