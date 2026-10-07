import 'package:flutter/material.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';
import 'replay_controller.dart';
import 'timeline/timeline_event_style.dart';

/// play button, clock, speed and a scrubber with a tick for every event
class ReplayControls extends StatelessWidget {
  const ReplayControls({
    super.key,
    required this.controller,
    required this.results,
  });

  final ReplayController controller;
  final GameResults results;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        final position = controller.positionSeconds;

        return Container(
          padding: const EdgeInsets.fromLTRB(
            XActSpace.s1,
            XActSpace.s1,
            XActSpace.s3,
            XActSpace.s1,
          ),
          decoration: BoxDecoration(
            color: XActColors.glass,
            borderRadius: XActRadius.md,
            border: Border.all(color: XActColors.hairlineSoft),
          ),
          child: Row(
            children: [
              IconButton(
                tooltip: controller.isPlaying ? 'Pause' : 'Play',
                onPressed: controller.toggle,
                icon: Icon(
                  controller.isPlaying
                      ? Icons.pause_rounded
                      : Icons.play_arrow_rounded,
                  color: XActColors.text1,
                ),
              ),
              Expanded(
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    Positioned.fill(
                      child: Padding(
                        // matches the slider's own track padding
                        padding: const EdgeInsets.symmetric(horizontal: 12),
                        child: CustomPaint(
                          painter: _EventTickPainter(results),
                        ),
                      ),
                    ),
                    SliderTheme(
                      data: SliderTheme.of(context).copyWith(
                        trackHeight: 3,
                        thumbShape: const RoundSliderThumbShape(
                          enabledThumbRadius: 7,
                        ),
                        overlayShape: SliderComponentShape.noOverlay,
                        activeTrackColor: XActColors.secondary,
                        inactiveTrackColor: XActColors.surface3,
                        thumbColor: Colors.white,
                      ),
                      child: Slider(
                        value: position.clamp(0, controller.matchSeconds.toDouble()),
                        max: controller.matchSeconds.toDouble(),
                        onChangeStart: (_) => controller.pause(),
                        onChanged: controller.seek,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: XActSpace.s2),
              Text(
                '${formatMatchClock(position.round())} / ${formatMatchClock(controller.matchSeconds)}',
                style: XActText.caption.copyWith(
                  color: XActColors.text2,
                  fontFeatures: const [FontFeature.tabularFigures()],
                ),
              ),
              const SizedBox(width: XActSpace.s2),
              ActionChip(
                visualDensity: VisualDensity.compact,
                label: Text('${controller.speed.round()}×'),
                onPressed: controller.cycleSpeed,
                tooltip: 'Playback speed',
              ),
            ],
          ),
        );
      },
    );
  }
}

class _EventTickPainter extends CustomPainter {
  _EventTickPainter(this.results);

  final GameResults results;

  @override
  void paint(Canvas canvas, Size size) {
    final total = results.durationSeconds <= 0 ? 1 : results.durationSeconds;
    final paint = Paint();
    for (final event in results.timeline) {
      if (event.type == TimelineEventType.gameStarted ||
          event.type == TimelineEventType.gameEnded) {
        continue;
      }
      final x = size.width * (event.offsetSeconds / total).clamp(0.0, 1.0);
      paint.color = TimelineEventStyle.of(results, event).color;
      final isCatch = event.type == TimelineEventType.mrXCaught;
      final height = isCatch ? 14.0 : 8.0;
      canvas.drawRRect(
        RRect.fromRectAndRadius(
          Rect.fromCenter(
            center: Offset(x, size.height / 2 - 9),
            width: isCatch ? 4 : 2,
            height: height,
          ),
          const Radius.circular(2),
        ),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(_EventTickPainter oldDelegate) =>
      oldDelegate.results != results;
}
