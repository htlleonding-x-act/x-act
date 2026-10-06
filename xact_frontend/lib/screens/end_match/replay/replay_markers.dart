import 'package:flutter/material.dart';

import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

class ReplayPlayerMarker extends StatelessWidget {
  const ReplayPlayerMarker({
    super.key,
    required this.name,
    required this.color,
    required this.isMrX,
    required this.focused,
  });

  final String name;
  final Color color;
  final bool isMrX;
  final bool focused;

  @override
  Widget build(BuildContext context) {
    final fill = isMrX ? XActColors.roleMrX : color;

    return Container(
      decoration: BoxDecoration(
        color: fill,
        shape: BoxShape.circle,
        border: Border.all(color: Colors.white, width: focused ? 3 : 2),
        boxShadow: [
          BoxShadow(
            color: fill.withValues(alpha: .6),
            blurRadius: focused ? 14 : 8,
          ),
        ],
      ),
      alignment: Alignment.center,
      child: isMrX
          ? const Icon(Icons.location_on_rounded, color: Colors.white, size: 16)
          : Text(
              initialOf(name),
              style: XActText.caption.copyWith(
                color: Colors.white,
                fontWeight: FontWeight.w800,
              ),
            ),
    );
  }
}

/// where the hunters saw mister x
class RevealPinMarker extends StatelessWidget {
  const RevealPinMarker({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: XActColors.roleMrX.withValues(alpha: .25),
        border: Border.all(color: XActColors.roleMrX, width: 1.5),
      ),
      child: const Center(
        child: Icon(Icons.visibility_rounded, size: 10, color: Colors.white),
      ),
    );
  }
}

class CatchMarker extends StatelessWidget {
  const CatchMarker({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: XActColors.warning,
        borderRadius: XActRadius.xs,
        border: Border.all(color: Colors.white, width: 2),
        boxShadow: XActElevation.e1,
      ),
      child: const Icon(Icons.back_hand_rounded, size: 14, color: Colors.white),
    );
  }
}

/// a ring that pulses a few times around whatever the timeline jumped to
class PulseHighlight extends StatefulWidget {
  const PulseHighlight({super.key, required this.color});

  final Color color;

  @override
  State<PulseHighlight> createState() => _PulseHighlightState();
}

class _PulseHighlightState extends State<PulseHighlight>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 700),
  )..repeat();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      builder: (context, _) => Transform.scale(
        scale: .4 + _controller.value * .8,
        child: Container(
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: Border.all(
              color: widget.color.withValues(alpha: 1 - _controller.value),
              width: 3,
            ),
          ),
        ),
      ),
    );
  }
}
