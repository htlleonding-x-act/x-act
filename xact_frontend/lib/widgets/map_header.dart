import 'dart:async';

import 'package:flutter/material.dart';

import '../api/api_service.dart';

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
          }
        }
      });
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

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 0,
      left: 0,
      right: 0,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        decoration: const BoxDecoration(
          color: Color(0xFF0F172A),
          borderRadius: BorderRadius.only(
            bottomLeft: Radius.circular(12),
            bottomRight: Radius.circular(12),
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'X-ACT',
                    style: TextStyle(
                      fontSize: 28,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                  FutureBuilder<MapHeaderData>(
                    future: _load,
                    builder: (context, snapshot) {
                      final text = snapshot.hasError
                          ? 'Next ping: unavailable'
                          : (_totalSeconds > 0
                                ? 'Next ping: $_countdownText'
                                : (snapshot.data?.nextPingText ??
                                      'Next ping: ...'));
                      return Row(
                        children: [
                          Icon(
                            Icons.access_time,
                            color: Colors.orange.shade400,
                            size: 20,
                          ),
                          const SizedBox(width: 6),
                          Text(
                            text,
                            style: TextStyle(
                              color: Colors.orange.shade400,
                              fontSize: 16,
                            ),
                          ),
                        ],
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
                    minHeight: 6,
                    backgroundColor: Colors.white12,
                    valueColor: AlwaysStoppedAnimation<Color>(
                      Colors.orange.shade400,
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
