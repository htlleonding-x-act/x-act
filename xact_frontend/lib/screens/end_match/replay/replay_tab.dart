import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import 'replay_controller.dart';
import 'replay_controls.dart';
import 'replay_map.dart';
import 'replay_member_chips.dart';
import 'replay_tracks.dart';
import 'timeline/match_timeline_list.dart';

class ReplayTab extends StatefulWidget {
  const ReplayTab({
    super.key,
    required this.results,
    required this.tracks,
    required this.controller,
    required this.mapController,
    required this.highlightedEvent,
    required this.onEventTap,
    required this.onMapReady,
  });

  final GameResults results;
  final ReplayTrackSet tracks;
  final ReplayController controller;
  final MapController mapController;
  final ValueNotifier<TimelineEvent?> highlightedEvent;
  final ValueChanged<TimelineEvent> onEventTap;
  final VoidCallback onMapReady;

  @override
  State<ReplayTab> createState() => _ReplayTabState();
}

class _ReplayTabState extends State<ReplayTab>
    with AutomaticKeepAliveClientMixin {
  // side by side above this width, map over feed below it
  static const double _wideBreakpoint = 900;
  static const double _timelineWidth = 360;

  // moving the camera every frame makes the tiles stutter
  static const Duration _followInterval = Duration(milliseconds: 250);

  bool _mapReady = false;
  DateTime _lastFollow = DateTime.fromMillisecondsSinceEpoch(0);

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_followFocusedMember);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_followFocusedMember);
    super.dispose();
  }

  void _followFocusedMember() {
    final memberId = widget.controller.focusedMemberId;
    if (!_mapReady || memberId == null) {
      return;
    }
    final now = DateTime.now();
    if (widget.controller.isPlaying && now.difference(_lastFollow) < _followInterval) {
      return;
    }

    final position = widget.tracks
        .trackOf(memberId)
        ?.positionAt(widget.controller.positionSeconds);
    if (position != null) {
      _lastFollow = now;
      widget.mapController.move(position, widget.mapController.camera.zoom);
    }
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);

    final map = ClipRRect(
      borderRadius: XActRadius.lg,
      child: Stack(
        children: [
          Positioned.fill(
            child: ReplayMap(
              results: widget.results,
              tracks: widget.tracks,
              controller: widget.controller,
              mapController: widget.mapController,
              highlightedEvent: widget.highlightedEvent,
              onMapReady: () {
                _mapReady = true;
                widget.onMapReady();
              },
            ),
          ),
          Positioned(
            left: 0,
            right: 0,
            top: XActSpace.s3,
            child: ReplayMemberChips(
              tracks: widget.tracks,
              controller: widget.controller,
            ),
          ),
          Positioned(
            left: XActSpace.s3,
            right: XActSpace.s3,
            bottom: XActSpace.s3,
            child: ReplayControls(
              controller: widget.controller,
              results: widget.results,
            ),
          ),
        ],
      ),
    );

    final timeline = Container(
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: MatchTimelineList(
        results: widget.results,
        controller: widget.controller,
        onEventTap: widget.onEventTap,
      ),
    );

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s3,
        XActSpace.s4,
        XActSpace.s3,
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          if (constraints.maxWidth >= _wideBreakpoint) {
            return Row(
              children: [
                Expanded(child: map),
                const SizedBox(width: XActSpace.s3),
                SizedBox(width: _timelineWidth, child: timeline),
              ],
            );
          }
          return Column(
            children: [
              Expanded(flex: 2, child: map),
              const SizedBox(height: XActSpace.s3),
              Expanded(child: timeline),
            ],
          );
        },
      ),
    );
  }
}
