import 'package:flutter/material.dart';

import '../../api/game_results.dart';
import '../../api/models.dart';
import '../../widgets/xact_branding.dart';

/// `12:04` or `1:02:04`
String formatMatchClock(int totalSeconds) {
  final seconds = totalSeconds < 0 ? 0 : totalSeconds;
  final hours = seconds ~/ 3600;
  final minutes = (seconds % 3600) ~/ 60;
  final rest = (seconds % 60).toString().padLeft(2, '0');
  if (hours > 0) {
    return '$hours:${minutes.toString().padLeft(2, '0')}:$rest';
  }
  return '$minutes:$rest';
}

String formatDistance(double meters) {
  if (meters < 1000) {
    return '${meters.round()} m';
  }
  return '${(meters / 1000).toStringAsFixed(meters < 10000 ? 2 : 1)} km';
}

String formatSpeed(double kmh) => '${kmh.toStringAsFixed(1)} km/h';

/// `6:12 /km`
String formatPace(double? secondsPerKm) {
  if (secondsPerKm == null) {
    return '–';
  }
  final total = secondsPerKm.round();
  return '${total ~/ 60}:${(total % 60).toString().padLeft(2, '0')} /km';
}

/// `1h 05m`, `12m` or `45s`
String formatShortDuration(Duration duration) {
  if (duration.inHours > 0) {
    final minutes = (duration.inMinutes % 60).toString().padLeft(2, '0');
    return '${duration.inHours}h ${minutes}m';
  }
  if (duration.inMinutes > 0) {
    return '${duration.inMinutes}m';
  }
  return '${duration.inSeconds}s';
}

String formatDateTime(DateTime value) {
  final local = value.toLocal();
  final day = local.day.toString().padLeft(2, '0');
  final month = local.month.toString().padLeft(2, '0');
  final hour = local.hour.toString().padLeft(2, '0');
  final minute = local.minute.toString().padLeft(2, '0');
  return '$day.$month.${local.year} $hour:$minute';
}

String endReasonLabel(GameEndReason reason) {
  return switch (reason) {
    GameEndReason.hostEnded => 'Ended by the host',
    GameEndReason.noOpponentsLeft => 'One side ran out of players',
    GameEndReason.hostLeft => 'The host left',
    GameEndReason.abandoned => 'Abandoned',
  };
}

// for teams that end up without a color of their own, never red so they can't
// be mistaken for mister x
const List<Color> _spareTeamColors = [
  Color(0xFFA855F7),
  Color(0xFF14B8A6),
  Color(0xFFEC4899),
  Color(0xFF84CC16),
  Color(0xFFF97316),
  Color(0xFF38BDF8),
];

final Expando<Map<int, Color>> _teamColorCache = Expando();

/// one distinct color per team for the whole end screen. a catch hands the
/// caught team the color of the catching team, so two teams can share the color
/// they hunted in. the lower team id keeps it and the other gets a spare one
Color teamAccent(GameResults results, int? teamId) {
  final colors = _teamColorCache[results] ??= _resolveTeamColors(results);
  return colors[teamId] ?? XActColors.roleSpectator;
}

Map<int, Color> _resolveTeamColors(GameResults results) {
  final colors = <int, Color>{};
  final used = <int>{XActColors.roleMrX.toARGB32()};
  final spares = _spareTeamColors.iterator;
  final teams = [...results.teams]..sort((a, b) => a.teamId.compareTo(b.teamId));

  for (final team in teams) {
    if (team.finalRole == TeamRole.spectator) {
      colors[team.teamId] = XActColors.roleSpectator;
    } else {
      var color = team.detectiveColorCode == null
          ? null
          : tryParseHexColor(team.detectiveColorCode!);
      if (color == null || used.contains(color.toARGB32())) {
        color = spares.moveNext() ? spares.current : XActColors.roleDetective;
      }
      used.add(color.toARGB32());
      colors[team.teamId] = color;
    }
  }

  return colors;
}

String initialOf(String name) {
  final trimmed = name.trim();
  return trimmed.isEmpty ? '?' : trimmed.characters.first.toUpperCase();
}
