import 'dart:async';

import 'package:flutter/services.dart';

import '../api/api_service.dart';
import '../api/models.dart';
import 'preferences_service.dart';

/// vibrates on the moments that matter in a match, so a player with the
/// phone in the pocket still notices them
final class GameHapticsService {
  GameHapticsService._();

  static final GameHapticsService instance = GameHapticsService._();

  // every member of the mister x team sends a revealed ping in the same
  // window, so one buzz covers the whole reveal
  static const _revealCooldown = Duration(seconds: 30);

  StreamSubscription<RealtimeEventEnvelope>? _subscription;
  DateTime? _lastReveal;

  void init() {
    _subscription ??= ApiService.instance.realtimeEvents.listen(
      _onRealtimeEvent,
    );
  }

  void _onRealtimeEvent(RealtimeEventEnvelope envelope) {
    if (!PreferencesService.instance.hapticFeedback.value) return;

    switch (envelope.type) {
      case RealtimeEvents.gameSessionStarted ||
          RealtimeEvents.gameSessionEnded ||
          RealtimeEvents.mrXCaught:
        unawaited(HapticFeedback.heavyImpact());
      case RealtimeEvents.locationLogRecorded
          when envelope.payload['isRevealedPosition'] == true:
        _onReveal();
    }
  }

  void _onReveal() {
    final now = DateTime.now();
    final last = _lastReveal;
    if (last != null && now.difference(last) < _revealCooldown) return;

    _lastReveal = now;
    unawaited(HapticFeedback.mediumImpact());
  }
}
