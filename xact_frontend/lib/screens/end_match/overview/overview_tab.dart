import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';
import 'award_card.dart';
import 'personal_summary_card.dart';
import 'stat_tile.dart';

class OverviewTab extends StatefulWidget {
  const OverviewTab({
    super.key,
    required this.results,
    required this.currentMemberId,
    this.onShare,
    this.onShowAward,
  });

  final GameResults results;
  final int? currentMemberId;
  final VoidCallback? onShare;
  final ValueChanged<MatchAward>? onShowAward;

  @override
  State<OverviewTab> createState() => _OverviewTabState();
}

class _OverviewTabState extends State<OverviewTab>
    with SingleTickerProviderStateMixin {
  late final AnimationController _awardsEntrance = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1200),
  );

  // each card starts a little later than the one before
  static const double _stagger = .08;
  static const double _cardShare = .4;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_awardsEntrance.status == AnimationStatus.dismissed) {
      if (MediaQuery.disableAnimationsOf(context)) {
        _awardsEntrance.value = 1;
      } else {
        _awardsEntrance.forward();
      }
    }
  }

  @override
  void dispose() {
    _awardsEntrance.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final results = widget.results;
    final me = results.memberById(widget.currentMemberId);
    final totalDistance = results.players.fold<double>(
      0,
      (sum, m) => sum + m.stats.distanceMeters,
    );
    final reveals = results.timeline
        .where((e) => e.type == TimelineEventType.mrXRevealed)
        .length;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s3,
        XActSpace.s4,
        XActSpace.s6,
      ),
      children: [
        StatTileGrid(
          tiles: [
            StatTile(
              label: 'Duration',
              value: formatShortDuration(results.duration),
              icon: Icons.timer_outlined,
              accent: XActColors.secondary,
            ),
            StatTile(
              label: 'Distance covered',
              value: formatDistance(totalDistance),
              icon: Icons.route_rounded,
              accent: XActColors.success,
            ),
            StatTile(
              label: 'Catches',
              value: '${results.catches.length}',
              icon: Icons.back_hand_rounded,
              accent: XActColors.warning,
            ),
            StatTile(
              label: 'Mister X reveals',
              value: '$reveals',
              icon: Icons.location_on_rounded,
              accent: XActColors.roleMrX,
            ),
          ],
        ),
        if (me != null) ...[
          const SizedBox(height: XActSpace.s4),
          PersonalSummaryCard(results: results, member: me),
        ],
        const SizedBox(height: XActSpace.s5),
        XActBranding.buildEyebrow('Awards'),
        const SizedBox(height: XActSpace.s3),
        ..._buildAwards(results),
        const SizedBox(height: XActSpace.s5),
        _buildMatchDetails(results),
        if (widget.onShare != null) ...[
          const SizedBox(height: XActSpace.s5),
          XActBranding.buildGhostButton(
            text: 'Share result',
            icon: Icons.ios_share_rounded,
            onPressed: widget.onShare,
          ),
        ],
      ],
    );
  }

  List<Widget> _buildAwards(GameResults results) {
    if (results.awards.isEmpty) {
      return [
        Text(
          'No awards this time. Move more, catch more!',
          style: XActText.bodySm.copyWith(color: XActColors.text3),
        ),
      ];
    }

    return [
      for (final (index, award) in results.awards.indexed)
        Padding(
          padding: const EdgeInsets.only(bottom: XActSpace.s3),
          child: AwardCard(
            award: award,
            winnerNames: [
              for (final id in award.memberIds)
                results.memberById(id)?.displayName ?? 'Unknown',
            ],
            entrance: _entranceFor(index),
            onShowOnReplay: widget.onShowAward == null
                ? null
                : () => widget.onShowAward!(award),
          ),
        ),
    ];
  }

  Animation<double> _entranceFor(int index) {
    final begin = (_stagger * index).clamp(0.0, 1 - _cardShare);
    return CurvedAnimation(
      parent: _awardsEntrance,
      curve: Interval(begin, begin + _cardShare, curve: Curves.easeOutCubic),
    );
  }

  Widget _buildMatchDetails(GameResults results) {
    return Container(
      padding: const EdgeInsets.all(XActSpace.s4),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          XActBranding.buildEyebrow('Match details'),
          const SizedBox(height: XActSpace.s3),
          _detail('Game', results.sessionName),
          _detail('Started', formatDateTime(results.startTime)),
          _detail('Ended', formatDateTime(results.endTime)),
          _detail('Reveal every', '${results.revealIntervalMinutes} min'),
          _detail('Players', '${results.players.length}'),
        ],
      ),
    );
  }

  Widget _detail(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          SizedBox(
            width: 110,
            child: Text(label, style: XActText.caption),
          ),
          Expanded(
            child: Text(
              value,
              style: XActText.bodySm,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }
}
