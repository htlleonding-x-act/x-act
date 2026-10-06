import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../../api/game_results.dart';
import '../../../widgets/xact_branding.dart';
import '../end_match_format.dart';

/// one player's route, prepared once so the replay only has to look things up
/// each frame
final class MemberTrack {
  MemberTrack._({
    required this.member,
    required this.color,
    required this.offsets,
    required this.latitudes,
    required this.longitudes,
    required this.mrXAtPoint,
  });

  factory MemberTrack.of(GameResults results, ResultMember member) {
    final route = member.route;
    return MemberTrack._(
      member: member,
      color: teamAccent(results, member.teamId),
      offsets: Int32List.fromList([for (final p in route) p.offsetSeconds]),
      latitudes: Float64List.fromList([
        for (final p in route) p.position.latitude,
      ]),
      longitudes: Float64List.fromList([
        for (final p in route) p.position.longitude,
      ]),
      mrXAtPoint: [
        for (final p in route) results.mrXTeamAt(p.offsetSeconds) == member.teamId,
      ],
    );
  }

  // pings come every few seconds, a longer silence is not a straight walk
  static const int _maxInterpolationGapSeconds = 120;

  final ResultMember member;
  final Color color;
  final Int32List offsets;
  final Float64List latitudes;
  final Float64List longitudes;
  final List<bool> mrXAtPoint;

  int _cachedTrailIndex = -2;
  List<Polyline> _cachedTrail = const [];
  List<Polyline>? _fullRoute;

  bool get isEmpty => offsets.isEmpty;

  LatLng pointAt(int index) => LatLng(latitudes[index], longitudes[index]);

  /// the last route point at or before [seconds], -1 before the first one
  int indexAt(double seconds) {
    var low = 0;
    var high = offsets.length - 1;
    var result = -1;
    while (low <= high) {
      final middle = (low + high) >> 1;
      if (offsets[middle] <= seconds) {
        result = middle;
        low = middle + 1;
      } else {
        high = middle - 1;
      }
    }
    return result;
  }

  LatLng? positionAt(double seconds) {
    final index = indexAt(seconds);
    if (index < 0) {
      return null;
    }
    if (index == offsets.length - 1) {
      return pointAt(index);
    }

    final gap = offsets[index + 1] - offsets[index];
    if (gap <= 0 || gap > _maxInterpolationGapSeconds) {
      return pointAt(index);
    }

    final t = (seconds - offsets[index]) / gap;
    return LatLng(
      latitudes[index] + (latitudes[index + 1] - latitudes[index]) * t,
      longitudes[index] + (longitudes[index + 1] - longitudes[index]) * t,
    );
  }

  bool isMrXAt(double seconds) {
    final index = indexAt(seconds);
    return index >= 0 && mrXAtPoint[index];
  }

  /// the route walked so far, split where the player switched between hunting
  /// and being mister x. cached per point, most frames reuse it
  List<Polyline> trailUpTo(int index) {
    if (index == _cachedTrailIndex) {
      return _cachedTrail;
    }
    _cachedTrailIndex = index;
    _cachedTrail = index < 1 ? const [] : _segments(index, alpha: 1);
    return _cachedTrail;
  }

  /// the whole route faded, so the player sees where everyone will go
  List<Polyline> fullRoute() =>
      _fullRoute ??= offsets.length < 2 ? const [] : _segments(offsets.length - 1, alpha: .14);

  List<Polyline> _segments(int lastIndex, {required double alpha}) {
    final polylines = <Polyline>[];
    var start = 0;
    for (var i = 1; i <= lastIndex; i++) {
      final switches = mrXAtPoint[i] != mrXAtPoint[start];
      if (switches || i == lastIndex) {
        final mrX = mrXAtPoint[start];
        polylines.add(
          Polyline(
            points: [for (var j = start; j <= i; j++) pointAt(j)],
            color: (mrX ? XActColors.roleMrX : color).withValues(alpha: alpha),
            strokeWidth: mrX ? 4 : 3,
          ),
        );
        start = i;
      }
    }
    return polylines;
  }
}

final class RevealPin {
  const RevealPin(this.offsetSeconds, this.position);

  final int offsetSeconds;
  final LatLng position;
}

final class ReplayTrackSet {
  ReplayTrackSet._(this.tracks, this.revealPins, this.bounds);

  factory ReplayTrackSet.of(GameResults results) {
    final tracks = [
      for (final member in results.players)
        if (member.route.isNotEmpty) MemberTrack.of(results, member),
    ];

    final revealPins = [
      for (final member in results.players)
        for (final point in member.route)
          if (point.isRevealed) RevealPin(point.offsetSeconds, point.position),
    ];

    final allPoints = [
      for (final member in results.players)
        for (final point in member.route) point.position,
      ...results.geofence,
    ];
    final bounds = allPoints.length >= 2
        ? LatLngBounds.fromPoints(allPoints)
        : null;

    return ReplayTrackSet._(tracks, revealPins, bounds);
  }

  final List<MemberTrack> tracks;
  final List<RevealPin> revealPins;

  /// null when there are not enough points to fit the camera to
  final LatLngBounds? bounds;

  MemberTrack? trackOf(int memberId) {
    for (final track in tracks) {
      if (track.member.memberId == memberId) return track;
    }
    return null;
  }
}
