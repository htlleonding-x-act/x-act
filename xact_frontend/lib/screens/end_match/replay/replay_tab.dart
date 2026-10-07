import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import 'replay_controller.dart';
import 'replay_controls.dart';
import 'replay_layers_sheet.dart';
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
    required this.fullscreen,
    required this.onToggleFullscreen,
  });

  final GameResults results;
  final ReplayTrackSet tracks;
  final ReplayController controller;
  final MapController mapController;
  final ValueNotifier<TimelineEvent?> highlightedEvent;
  final ValueChanged<TimelineEvent> onEventTap;
  final VoidCallback onMapReady;

  /// the map alone over the whole screen, so a phone can show all of it
  final bool fullscreen;
  final VoidCallback onToggleFullscreen;

  @override
  State<ReplayTab> createState() => _ReplayTabState();
}

class _ReplayTabState extends State<ReplayTab> {
  // side by side above this width, map over feed below it. a short and wide
  // screen like a phone in landscape also goes side by side, stacked the map
  // would be too flat to use
  static const double _wideBreakpoint = 900;
  static const double _landscapeMinWidth = 560;
  static const double _landscapeAspectRatio = 2;
  static const double _timelineWidth = 360;
  static const double _timelineMaxShare = .4;

  // moving the camera every frame makes the tiles stutter
  static const Duration _followInterval = Duration(milliseconds: 250);

  // the map moves between the row, the column and fullscreen. the key keeps
  // its state, so the camera stays where it was
  final GlobalKey _mapKey = GlobalKey();
  bool _mapReady = false;
  DateTime _lastFollow = DateTime.fromMillisecondsSinceEpoch(0);

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
    final map = ClipRRect(
      key: _mapKey,
      borderRadius: widget.fullscreen ? BorderRadius.zero : XActRadius.lg,
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
            right: XActSpace.s3,
            top: XActSpace.s3,
            child: Row(
              children: [
                Expanded(
                  child: ReplayMemberChips(
                    tracks: widget.tracks,
                    controller: widget.controller,
                  ),
                ),
                _MapButton(
                  tooltip: 'Layers and players',
                  icon: Icons.layers_rounded,
                  onPressed: () => showReplayLayersSheet(
                    context,
                    tracks: widget.tracks,
                    visibility: widget.controller.visibility,
                  ),
                ),
                const SizedBox(width: XActSpace.s2),
                _MapButton(
                  tooltip: widget.fullscreen ? 'Exit fullscreen' : 'Fullscreen',
                  icon: widget.fullscreen
                      ? Icons.fullscreen_exit_rounded
                      : Icons.fullscreen_rounded,
                  onPressed: widget.onToggleFullscreen,
                ),
              ],
            ),
          ),
          Positioned(
            left: XActSpace.s3,
            right: XActSpace.s3,
            // the end screen leaves the bottom inset to its action bar, which
            // fullscreen hides
            bottom:
                XActSpace.s3 +
                (widget.fullscreen ? MediaQuery.paddingOf(context).bottom : 0),
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

    if (widget.fullscreen) {
      return map;
    }

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s3,
        XActSpace.s4,
        XActSpace.s3,
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final width = constraints.maxWidth;
          final sideBySide =
              width >= _wideBreakpoint ||
              (width >= _landscapeMinWidth &&
                  width >= constraints.maxHeight * _landscapeAspectRatio);
          if (sideBySide) {
            return Row(
              children: [
                Expanded(child: map),
                const SizedBox(width: XActSpace.s3),
                SizedBox(
                  width: min(_timelineWidth, width * _timelineMaxShare),
                  child: timeline,
                ),
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

class _MapButton extends StatelessWidget {
  const _MapButton({
    required this.tooltip,
    required this.icon,
    required this.onPressed,
  });

  final String tooltip;
  final IconData icon;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: XActColors.glass,
      shape: CircleBorder(side: BorderSide(color: XActColors.hairlineSoft)),
      child: IconButton(
        tooltip: tooltip,
        onPressed: onPressed,
        visualDensity: VisualDensity.compact,
        icon: Icon(icon, color: XActColors.text1, size: 20),
      ),
    );
  }
}
