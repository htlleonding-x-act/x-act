import 'dart:math';

import 'package:flutter/material.dart';

/// a one-shot ring of particles flying out of the winner icon
class HeroBurstPainter extends CustomPainter {
  HeroBurstPainter({
    required this.progress,
    required this.color,
    required int seed,
  }) : _particles = _buildParticles(seed);

  final double progress;
  final Color color;
  final List<_Particle> _particles;

  static const int _particleCount = 36;

  static List<_Particle> _buildParticles(int seed) {
    final random = Random(seed);
    return List.generate(_particleCount, (i) {
      final angle = i / _particleCount * 2 * pi + random.nextDouble() * .3;
      return _Particle(
        angle: angle,
        reach: .55 + random.nextDouble() * .45,
        radius: 1.5 + random.nextDouble() * 2.5,
        light: random.nextBool(),
      );
    });
  }

  @override
  void paint(Canvas canvas, Size size) {
    if (progress <= 0 || progress >= 1) {
      return;
    }

    final center = size.center(Offset.zero);
    final maxDistance = size.shortestSide / 2;
    final eased = Curves.easeOutCubic.transform(progress);
    final opacity = (1 - progress).clamp(0.0, 1.0);
    final paint = Paint();

    for (final particle in _particles) {
      final distance = maxDistance * particle.reach * eased;
      final offset =
          center +
          Offset(cos(particle.angle), sin(particle.angle)) * distance;
      paint.color = (particle.light ? Colors.white : color).withValues(
        alpha: opacity,
      );
      canvas.drawCircle(offset, particle.radius * (1 - eased * .5), paint);
    }
  }

  @override
  bool shouldRepaint(HeroBurstPainter oldDelegate) =>
      oldDelegate.progress != progress || oldDelegate.color != color;
}

final class _Particle {
  const _Particle({
    required this.angle,
    required this.reach,
    required this.radius,
    required this.light,
  });

  final double angle;
  final double reach;
  final double radius;
  final bool light;
}
