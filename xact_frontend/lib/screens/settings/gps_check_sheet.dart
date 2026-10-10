import 'dart:async';

import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/services/location_service.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

enum _GpsState { starting, noPermission, serviceOff, waiting, fix }

/// shows the live accuracy of the gps so a player can find a better spot
/// before the match starts
class GpsCheckSheet extends StatefulWidget {
  const GpsCheckSheet({super.key});

  static Future<void> show(BuildContext context) {
    return showModalBottomSheet<void>(
      context: context,
      backgroundColor: XActColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => const GpsCheckSheet(),
    );
  }

  @override
  State<GpsCheckSheet> createState() => _GpsCheckSheetState();
}

class _GpsCheckSheetState extends State<GpsCheckSheet> {
  // in meters; matches what the map can still place on the right street
  static const _goodAccuracy = 15.0;
  static const _okAccuracy = 40.0;

  // a stream of its own, so the check never touches the tracking of a
  // running match
  StreamSubscription<Position>? _subscription;
  _GpsState _state = _GpsState.starting;
  Position? _position;

  @override
  void initState() {
    super.initState();
    _start();
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }

  Future<void> _start() async {
    await _subscription?.cancel();
    _subscription = null;
    setState(() => _state = _GpsState.starting);

    final granted = await LocationService.instance.requestPermission();
    if (!mounted) return;
    if (!granted) {
      setState(() => _state = _GpsState.noPermission);
      return;
    }

    final serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!mounted) return;
    if (!serviceEnabled) {
      setState(() => _state = _GpsState.serviceOff);
      return;
    }

    setState(() => _state = _GpsState.waiting);
    _subscription =
        Geolocator.getPositionStream(
          locationSettings: const LocationSettings(
            accuracy: LocationAccuracy.best,
          ),
        ).listen(
          (position) => setState(() {
            _position = position;
            _state = _GpsState.fix;
          }),
          onError: (_) => setState(() => _state = _GpsState.serviceOff),
        );
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 20, 24, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            XActBranding.buildEyebrow('Location'),
            const SizedBox(height: 4),
            Text('GPS check', style: XActText.heading),
            const SizedBox(height: 24),
            _buildBody(),
          ],
        ),
      ),
    );
  }

  Widget _buildBody() {
    return switch (_state) {
      _GpsState.starting || _GpsState.waiting => _buildMessage(
        icon: Icons.satellite_alt_rounded,
        text: 'Looking for satellites…',
        showProgress: true,
      ),
      _GpsState.noPermission => _buildMessage(
        icon: Icons.location_disabled_rounded,
        text: 'X-ACT is not allowed to use your location.',
        actionText: 'Open app settings',
        onAction: () => _openSystemSettings(Geolocator.openAppSettings),
      ),
      _GpsState.serviceOff => _buildMessage(
        icon: Icons.location_off_rounded,
        text: 'Location is turned off on this device.',
        actionText: 'Open location settings',
        onAction: () => _openSystemSettings(Geolocator.openLocationSettings),
      ),
      _GpsState.fix => _buildAccuracy(_position!),
    };
  }

  /// the browser has no settings page to open, so this only retries there
  Future<void> _openSystemSettings(Future<bool> Function() open) async {
    try {
      await open();
    } catch (_) {}
    if (mounted) _start();
  }

  Widget _buildMessage({
    required IconData icon,
    required String text,
    bool showProgress = false,
    String? actionText,
    VoidCallback? onAction,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Icon(icon, size: 40, color: XActColors.text3),
        const SizedBox(height: 12),
        Text(
          text,
          textAlign: TextAlign.center,
          style: XActText.body.copyWith(color: XActColors.text2),
        ),
        if (showProgress) ...[
          const SizedBox(height: 16),
          const Center(child: CircularProgressIndicator()),
        ],
        if (actionText != null) ...[
          const SizedBox(height: 20),
          XActBranding.buildSecondaryButton(
            text: actionText,
            onPressed: onAction,
          ),
        ],
      ],
    );
  }

  Widget _buildAccuracy(Position position) {
    final accuracy = position.accuracy;
    final (label, hint, color) = switch (accuracy) {
      < _goodAccuracy => (
        'Good',
        'Your position is precise enough to play.',
        XActColors.success,
      ),
      < _okAccuracy => (
        'Okay',
        'Playable, but others may see you a street off.',
        XActColors.warning,
      ),
      _ => (
        'Poor',
        'Move away from tall buildings or step outside.',
        XActColors.primary,
      ),
    };

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Center(
          child: Text(
            '± ${accuracy.round()} m',
            style: GoogleFonts.spaceGrotesk(
              fontSize: 44,
              fontWeight: FontWeight.w700,
              color: XActColors.text1,
            ),
          ),
        ),
        const SizedBox(height: 8),
        Center(
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              color: color.withValues(alpha: .16),
              borderRadius: XActRadius.pill,
            ),
            child: Text(
              label.toUpperCase(),
              style: GoogleFonts.inter(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: color,
                letterSpacing: 1.2,
              ),
            ),
          ),
        ),
        const SizedBox(height: 12),
        Text(
          hint,
          textAlign: TextAlign.center,
          style: XActText.bodySm.copyWith(color: XActColors.text3),
        ),
        const SizedBox(height: 4),
        Text(
          'Updates live while this sheet is open.',
          textAlign: TextAlign.center,
          style: XActText.caption,
        ),
      ],
    );
  }
}
