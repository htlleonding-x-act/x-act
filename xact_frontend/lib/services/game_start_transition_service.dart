import 'dart:async';

import 'package:flutter/foundation.dart';

final class GameStartTransitionState {
  final bool isVisible;
  final int? secondsRemaining;
  final bool isGoPhase;

  const GameStartTransitionState({
    required this.isVisible,
    required this.secondsRemaining,
    required this.isGoPhase,
  });

  static const hidden = GameStartTransitionState(
    isVisible: false,
    secondsRemaining: null,
    isGoPhase: false,
  );
}

final class GameStartTransitionService {
  GameStartTransitionService._();

  static final GameStartTransitionService instance =
      GameStartTransitionService._();

  final ValueNotifier<GameStartTransitionState> state =
      ValueNotifier<GameStartTransitionState>(GameStartTransitionState.hidden);

  Future<void>? _activeTransition;

  Future<void> playCountdown({int seconds = 3}) {
    if (_activeTransition != null) {
      return _activeTransition!;
    }

    final completer = Completer<void>();
    _activeTransition = completer.future;

    _run(seconds, completer);
    return completer.future;
  }

  Future<void> _run(int seconds, Completer<void> completer) async {
    try {
      for (var i = seconds; i >= 1; i--) {
        state.value = GameStartTransitionState(
          isVisible: true,
          secondsRemaining: i,
          isGoPhase: false,
        );
        await Future<void>.delayed(const Duration(seconds: 1));
      }

      state.value = const GameStartTransitionState(
        isVisible: true,
        secondsRemaining: null,
        isGoPhase: true,
      );
      await Future<void>.delayed(const Duration(milliseconds: 650));
    } finally {
      state.value = GameStartTransitionState.hidden;
      _activeTransition = null;
      if (!completer.isCompleted) {
        completer.complete();
      }
    }
  }
}
