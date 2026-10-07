import 'package:flutter/animation.dart';
import 'package:flutter/foundation.dart';

import 'replay_visibility.dart';

/// the playback clock of the replay. one animation controller runs from 0 to 1
/// over the match, its duration is the match length divided by the speed
class ReplayController extends ChangeNotifier {
  ReplayController({required TickerProvider vsync, required int matchSeconds})
    : matchSeconds = matchSeconds <= 0 ? 1 : matchSeconds {
    _speed = _defaultSpeed(this.matchSeconds);
    _animation =
        AnimationController(vsync: vsync, duration: _durationFor(_speed))
          ..addListener(notifyListeners)
          ..addStatusListener((_) => notifyListeners());
    visibility.addListener(_onVisibilityChanged);
  }

  static const List<double> speeds = [30, 60, 120, 240];

  // a whole match should play back in about this long at the default speed
  static const double _targetPlaybackSeconds = 45;

  final int matchSeconds;
  late final AnimationController _animation;
  late double _speed;
  final ReplayVisibility visibility = ReplayVisibility();
  int? _focusedMemberId;

  double get positionSeconds => _animation.value * matchSeconds;
  bool get isPlaying => _animation.isAnimating;
  double get speed => _speed;
  int? get focusedMemberId => _focusedMemberId;

  static double _defaultSpeed(int matchSeconds) {
    final wanted = matchSeconds / _targetPlaybackSeconds;
    return speeds.reduce(
      (a, b) => (a - wanted).abs() <= (b - wanted).abs() ? a : b,
    );
  }

  Duration _durationFor(double speed) =>
      Duration(milliseconds: (matchSeconds / speed * 1000).round());

  void play() {
    if (_animation.value >= 1) {
      _animation.value = 0;
    }
    _animation.forward();
  }

  void pause() {
    if (_animation.isAnimating) {
      _animation.stop();
      notifyListeners();
    }
  }

  void toggle() => isPlaying ? pause() : play();

  void seek(double seconds) {
    final wasPlaying = isPlaying;
    _animation.value = (seconds / matchSeconds).clamp(0.0, 1.0);
    if (wasPlaying) {
      _animation.forward();
    }
  }

  void cycleSpeed() {
    final next = speeds[(speeds.indexOf(_speed) + 1) % speeds.length];
    final wasPlaying = isPlaying;
    _speed = next;
    _animation.duration = _durationFor(next);
    if (wasPlaying) {
      // forward picks up the new duration from the current value
      _animation.forward();
    }
    notifyListeners();
  }

  void focusMember(int? memberId) {
    _focusedMemberId = memberId;
    if (memberId != null) {
      visibility.showMember(memberId);
    }
    notifyListeners();
  }

  /// the camera can't follow someone who is no longer drawn
  void _onVisibilityChanged() {
    if (_focusedMemberId case final memberId?
        when visibility.isMemberHidden(memberId)) {
      _focusedMemberId = null;
    }
    notifyListeners();
  }

  @override
  void dispose() {
    visibility.dispose();
    _animation.dispose();
    super.dispose();
  }
}
