import 'dart:async';

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

  /// Seconds left until the next ping – drives the progress bar.
  int _secondsRemaining = 0;

  /// Total interval in seconds.
  int _totalSeconds = 0;

  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _load = ApiService.instance.loadMapHeader()..then(_startCountdown);
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  void _startCountdown(MapHeaderData data) {
    if (!mounted) return;
    setState(() {
      _totalSeconds = data.intervalSeconds;
      _secondsRemaining = data.remainingSeconds;
    });

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
    final data = await ApiService.instance.loadMapHeader();
    if (!mounted) return;
    _startCountdown(data);
  }

  double get _progress =>
      _totalSeconds > 0 ? 1.0 - (_secondsRemaining / _totalSeconds) : 0.0;

  String get _countdownText {
    if (_totalSeconds <= 0) return '';
    final m = _secondsRemaining ~/ 60;
    final s = _secondsRemaining % 60;
    return '${m}m ${s.toString().padLeft(2, '0')}s';
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
                              snapshot.hasError
                                  ? 'unavailable'
                                  : (_totalSeconds > 0
                                      ? _countdownText
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
