import 'dart:math';

import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart' show LatLng;

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

/// every route drawn on a plain canvas. no map tiles, so the card can turn into
/// an image without loading anything from the network
class RouteSketchPainter extends CustomPainter {
  RouteSketchPainter(this.results);

  final GameResults results;

  static const double _padding = 12;

  @override
  void paint(Canvas canvas, Size size) {
    final points = [
      for (final member in results.players)
        for (final point in member.route) point.position,
      ...results.geofence,
    ];
    if (points.length < 2) {
      return;
    }

    // equirectangular is exact enough for a few kilometers
    final meanLatitude =
        points.map((p) => p.latitude).reduce((a, b) => a + b) / points.length;
    final xScale = cos(meanLatitude * pi / 180);
    double xOf(LatLng p) => p.longitude * xScale;
    double yOf(LatLng p) => -p.latitude;

    final minX = points.map(xOf).reduce(min);
    final maxX = points.map(xOf).reduce(max);
    final minY = points.map(yOf).reduce(min);
    final maxY = points.map(yOf).reduce(max);
    final spanX = max(maxX - minX, 1e-9);
    final spanY = max(maxY - minY, 1e-9);
    final scale = min(
      (size.width - 2 * _padding) / spanX,
      (size.height - 2 * _padding) / spanY,
    );
    final offsetX = (size.width - spanX * scale) / 2;
    final offsetY = (size.height - spanY * scale) / 2;
    Offset project(LatLng p) => Offset(
      offsetX + (xOf(p) - minX) * scale,
      offsetY + (yOf(p) - minY) * scale,
    );

    if (results.geofence.length >= 3) {
      final fence = Path()
        ..addPolygon(results.geofence.map(project).toList(), true);
      canvas.drawPath(
        fence,
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = 1.2
          ..color = XActColors.secondary.withValues(alpha: .45),
      );
    }

    final stroke = Paint()
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;

    // detectives first, so mister x stays on top
    for (final drawMrX in [false, true]) {
      for (final member in results.players) {
        _drawRoute(canvas, member, project, stroke, drawMrX: drawMrX);
      }
    }

    final dot = Paint()..color = XActColors.warning;
    for (final event in results.catches) {
      if (event.position case final position?) {
        canvas.drawCircle(project(position), 4.5, dot);
        canvas.drawCircle(
          project(position),
          4.5,
          Paint()
            ..style = PaintingStyle.stroke
            ..strokeWidth = 1.5
            ..color = Colors.white,
        );
      }
    }
  }

  void _drawRoute(
    Canvas canvas,
    ResultMember member,
    Offset Function(LatLng) project,
    Paint stroke, {
    required bool drawMrX,
  }) {
    final route = member.route;
    final color = teamAccent(results, member.teamId);
    for (var i = 1; i < route.length; i++) {
      final isMrX =
          results.mrXTeamAt(route[i - 1].offsetSeconds) == member.teamId;
      if (isMrX == drawMrX) {
        stroke
          ..color = isMrX ? XActColors.roleMrX : color.withValues(alpha: .75)
          ..strokeWidth = isMrX ? 2.4 : 1.4;
        canvas.drawLine(
          project(route[i - 1].position),
          project(route[i].position),
          stroke,
        );
      }
    }
  }

  @override
  bool shouldRepaint(RouteSketchPainter oldDelegate) =>
      oldDelegate.results != results;
}
