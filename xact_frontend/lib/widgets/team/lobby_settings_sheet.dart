import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/widgets/team/setting_slider_card.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

typedef LobbySettings = ({int plannedDurationMinutes, int mrXRevealInterval});

class LobbySettingsSheet extends StatefulWidget {
  final int sessionId;
  final LobbySettings initialSettings;
  final VoidCallback? onEditMap;

  const LobbySettingsSheet({
    super.key,
    required this.sessionId,
    required this.initialSettings,
    this.onEditMap,
  });

  static Future<LobbySettings?> show({
    required BuildContext context,
    required int sessionId,
    required LobbySettings initialSettings,
    VoidCallback? onEditMap,
  }) {
    return showModalBottomSheet<LobbySettings>(
      context: context,
      backgroundColor: XActColors.surface,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => LobbySettingsSheet(
        sessionId: sessionId,
        initialSettings: initialSettings,
        onEditMap: onEditMap,
      ),
    );
  }

  @override
  State<LobbySettingsSheet> createState() => _LobbySettingsSheetState();
}

class _LobbySettingsSheetState extends State<LobbySettingsSheet> {
  static const int _maxPingInterval = 30;

  late int _matchMinutes;
  late int _interval;
  bool _saving = false;

  // the backend only accepts a ping interval shorter than the match
  int get _maxInterval => math.min(_maxPingInterval, _matchMinutes - 1);

  @override
  void initState() {
    super.initState();
    _matchMinutes = widget.initialSettings.plannedDurationMinutes.clamp(
      MatchLengthCard.minMinutes,
      MatchLengthCard.maxMinutes,
    );
    _interval = widget.initialSettings.mrXRevealInterval.clamp(1, _maxInterval);
  }

  void _onMatchMinutesChanged(int minutes) {
    setState(() {
      _matchMinutes = minutes;
      _interval = _interval.clamp(1, _maxInterval);
    });
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      await ApiService.instance.updateSessionSettings(
        sessionId: widget.sessionId,
        plannedDurationMinutes: _matchMinutes,
        mrXRevealInterval: _interval,
      );
      if (mounted) {
        Navigator.of(context).pop((
          plannedDurationMinutes: _matchMinutes,
          mrXRevealInterval: _interval,
        ));
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not save settings. ${describeApiError(e)}')));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 36,
                height: 4,
                decoration: BoxDecoration(
                  color: XActColors.hairlineSoft,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 20),
            Text('Game Settings', style: XActText.heading),
            const SizedBox(height: 16),
            MatchLengthCard(
              minutes: _matchMinutes,
              onChanged: _onMatchMinutesChanged,
            ),
            const SizedBox(height: 12),
            SettingSliderCard(
              title: 'Ping Interval',
              caption: 'Mister X revealed every N minutes',
              minutes: _interval,
              min: 1,
              max: _maxInterval,
              onChanged: (v) => setState(() => _interval = v),
            ),
            const SizedBox(height: 12),
            XActBranding.buildGhostButton(
              text: 'Edit Map Area',
              icon: Icons.edit_location_alt_rounded,
              height: 48,
              onPressed: widget.onEditMap,
            ),
            const SizedBox(height: 10),
            XActBranding.buildSecondaryButton(
              text: 'Save Settings',
              icon: Icons.check_rounded,
              onPressed: _saving ? null : _save,
              height: 52,
            ),
          ],
        ),
      ),
    );
  }
}
