import 'dart:async';

import 'package:flutter/material.dart';

import '../../../../api/game_results.dart';
import '../../../../widgets/xact_branding.dart';
import '../../end_match_format.dart';
import '../replay_controller.dart';
import 'timeline_event_style.dart';

/// the match as a feed. the event the replay just passed is highlighted and
/// scrolled into view, tapping one jumps the replay there
class MatchTimelineList extends StatefulWidget {
  const MatchTimelineList({
    super.key,
    required this.results,
    required this.controller,
    required this.onEventTap,
  });

  final GameResults results;
  final ReplayController controller;
  final ValueChanged<TimelineEvent> onEventTap;

  @override
  State<MatchTimelineList> createState() => _MatchTimelineListState();
}

class _MatchTimelineListState extends State<MatchTimelineList> {
  // fixed so the list can compute where each row sits
  static const double _itemExtent = 64;

  // after the player scrolls by hand the list stops following for a moment
  static const Duration _manualScrollPause = Duration(seconds: 4);

  final ScrollController _scrollController = ScrollController();
  int _activeIndex = -1;
  DateTime _followAfter = DateTime.fromMillisecondsSinceEpoch(0);

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onPositionChanged);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onPositionChanged);
    _scrollController.dispose();
    super.dispose();
  }

  void _onPositionChanged() {
    final seconds = widget.controller.positionSeconds;
    final timeline = widget.results.timeline;
    var active = -1;
    for (var i = 0; i < timeline.length && timeline[i].offsetSeconds <= seconds; i++) {
      active = i;
    }
    if (active == _activeIndex) {
      return;
    }

    setState(() => _activeIndex = active);
    if (widget.controller.isPlaying &&
        DateTime.now().isAfter(_followAfter) &&
        _scrollController.hasClients &&
        active >= 0) {
      final viewport = _scrollController.position.viewportDimension;
      final target = (active * _itemExtent - viewport / 3).clamp(
        0.0,
        _scrollController.position.maxScrollExtent,
      );
      unawaited(
        _scrollController.animateTo(
          target,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final timeline = widget.results.timeline;

    return NotificationListener<UserScrollNotification>(
      onNotification: (_) {
        _followAfter = DateTime.now().add(_manualScrollPause);
        return false;
      },
      child: ListView.builder(
        controller: _scrollController,
        itemExtent: _itemExtent,
        padding: const EdgeInsets.symmetric(vertical: XActSpace.s2),
        itemCount: timeline.length,
        itemBuilder: (context, index) => _MatchTimelineTile(
          event: timeline[index],
          style: TimelineEventStyle.of(widget.results, timeline[index]),
          active: index == _activeIndex,
          passed: index <= _activeIndex,
          onTap: () => widget.onEventTap(timeline[index]),
        ),
      ),
    );
  }
}

class _MatchTimelineTile extends StatelessWidget {
  const _MatchTimelineTile({
    required this.event,
    required this.style,
    required this.active,
    required this.passed,
    required this.onTap,
  });

  final TimelineEvent event;
  final TimelineEventStyle style;
  final bool active;
  final bool passed;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final subtitle = style.subtitle;

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: XActSpace.s3, vertical: 3),
      child: Material(
        color: active ? style.color.withValues(alpha: .14) : Colors.transparent,
        borderRadius: XActRadius.sm,
        child: InkWell(
          borderRadius: XActRadius.sm,
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: XActSpace.s2),
            child: Row(
              children: [
                SizedBox(
                  width: 46,
                  child: Text(
                    formatMatchClock(event.offsetSeconds),
                    style: XActText.caption.copyWith(
                      color: active ? XActColors.text1 : XActColors.text4,
                      fontFeatures: const [FontFeature.tabularFigures()],
                    ),
                  ),
                ),
                Container(
                  width: 30,
                  height: 30,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: style.color.withValues(alpha: passed ? .22 : .08),
                    border: Border.all(
                      color: style.color.withValues(alpha: passed ? .8 : .3),
                    ),
                  ),
                  child: Icon(
                    style.icon,
                    size: 15,
                    color: passed ? style.color : style.color.withValues(alpha: .5),
                  ),
                ),
                const SizedBox(width: XActSpace.s3),
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        style.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: XActText.bodySm.copyWith(
                          color: passed ? XActColors.text1 : XActColors.text3,
                          fontWeight: active ? FontWeight.w700 : FontWeight.w500,
                        ),
                      ),
                      if (subtitle != null && subtitle.isNotEmpty)
                        Text(
                          subtitle,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: XActText.caption,
                        ),
                    ],
                  ),
                ),
                if (event.position != null)
                  Icon(Icons.my_location_rounded, size: 14, color: XActColors.text5),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
