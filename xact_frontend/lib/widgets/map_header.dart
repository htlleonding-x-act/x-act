import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../api/api_service.dart';
import 'xact_branding.dart';

class MapHeader extends StatefulWidget {
  const MapHeader({super.key});

  @override
  State<MapHeader> createState() => _MapHeaderState();
}

class _MapHeaderState extends State<MapHeader> {
  late final Future<MapHeaderData> _load;

  /// until the next ping, drives the progress bar
  int _secondsRemaining = 0;

  /// length of the whole ping interval
  int _totalSeconds = 0;

  Timer? _timer;

  /// on the local clock, so the match clock stays right even when the timer
  /// skips ticks while the phone is locked
  DateTime? _matchEndsAt;

  Timer? _clockTimer;

  @override
  void initState() {
    super.initState();
    _load = ApiService.instance.loadMapHeader();
    unawaited(
      _load.then(_startCountdown, onError: (Object _) => _retryRefreshSoon()),
    );
  }

  @override
  void dispose() {
    _timer?.cancel();
    _clockTimer?.cancel();
    super.dispose();
  }

  void _startCountdown(MapHeaderData data) {
    if (!mounted) return;
    final matchSecondsLeft = data.matchSecondsLeft;
    setState(() {
      _totalSeconds = data.intervalSeconds;
      _secondsRemaining = data.remainingSeconds;
      _matchEndsAt = matchSecondsLeft == null
          ? null
          : DateTime.now().add(Duration(seconds: matchSecondsLeft));
    });

    if (_matchEndsAt != null) {
      _clockTimer ??= Timer.periodic(const Duration(seconds: 1), (_) {
        if (mounted) setState(() {});
      });
    }

    _timer?.cancel();
    if (_totalSeconds <= 0) return;

    if (_secondsRemaining <= 0) {
      _timer = Timer(const Duration(seconds: 1), () {
        if (mounted) {
          unawaited(_refreshCountdown());
        }
      });
      return;
    }

    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) {
        _timer?.cancel();
        return;
      }
      setState(() {
        if (_secondsRemaining > 0) {
          _secondsRemaining--;
          if (_secondsRemaining == 0) {
            _timer?.cancel();
            _timer = Timer(const Duration(seconds: 1), () {
              if (mounted) {
                unawaited(_refreshCountdown());
              }
            });
          }
        }
      });
    });
  }

  Future<void> _refreshCountdown() async {
    try {
      final data = await ApiService.instance.loadMapHeader();
      if (!mounted) return;
      _startCountdown(data);
    } catch (_) {
      _retryRefreshSoon();
    }
  }

  void _retryRefreshSoon() {
    if (!mounted) return;
    _timer?.cancel();
    _timer = Timer(const Duration(seconds: 2), () {
      if (mounted) {
        unawaited(_refreshCountdown());
      }
    });
  }

  double get _progress =>
      _totalSeconds > 0 ? 1.0 - (_secondsRemaining / _totalSeconds) : 0.0;

  String get _countdownText {
    if (_totalSeconds <= 0) return '';
    final m = _secondsRemaining ~/ 60;
    final s = _secondsRemaining % 60;
    return '${m}m ${s.toString().padLeft(2, '0')}s';
  }

  int get _matchSecondsLeft {
    final endsAt = _matchEndsAt;
    if (endsAt == null) return 0;
    return math.max(0, endsAt.difference(DateTime.now()).inSeconds);
  }

  String get _matchClockText {
    final left = _matchSecondsLeft;
    final h = left ~/ 3600;
    final m = (left % 3600) ~/ 60;
    final s = (left % 60).toString().padLeft(2, '0');
    return h > 0 ? '$h:${m.toString().padLeft(2, '0')}:$s' : '$m:$s';
  }

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 12,
      left: 12,
      right: 12,
      child: SafeArea(
        bottom: false,
        child: Container(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 14),
          decoration: BoxDecoration(
            color: XActColors.glass,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: XActColors.hairlineSoft),
            boxShadow: XActElevation.e2,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Container(
                    width: 38,
                    height: 38,
                    decoration: BoxDecoration(
                      color: XActColors.primarySoft,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(
                      Icons.timer_outlined,
                      color: XActColors.primary,
                      size: 20,
                    ),
                  ),
                  const SizedBox(width: 12),
                  FutureBuilder<MapHeaderData>(
                    future: _load,
                    builder: (context, snapshot) {
                      return Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            XActBranding.buildEyebrow('Next ping'),
                            const SizedBox(height: 2),
                            Text(
                              _totalSeconds > 0
                                  ? _countdownText
                                  : (snapshot.hasError
                                      ? 'unavailable'
                                      : (snapshot.data?.nextPingText ?? '…')),
                              style: XActText.mono.copyWith(
                                fontSize: 20,
                                color: XActColors.text1,
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
                  if (_matchEndsAt != null) ...[
                    const SizedBox(width: 12),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        XActBranding.buildEyebrow('Time left'),
                        const SizedBox(height: 2),
                        Text(
                          _matchClockText,
                          style: XActText.mono.copyWith(
                            fontSize: 20,
                            color: _matchSecondsLeft < 5 * 60
                                ? XActColors.warning
                                : XActColors.text1,
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
              if (_totalSeconds > 0) ...[
                const SizedBox(height: 10),
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(
                    value: _progress,
                    minHeight: 4,
                    backgroundColor: Colors.white.withValues(alpha: .08),
                    valueColor: const AlwaysStoppedAnimation<Color>(
                      XActColors.primary,
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
