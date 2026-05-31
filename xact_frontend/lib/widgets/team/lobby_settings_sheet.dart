import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class LobbySettingsSheet extends StatefulWidget {
  final int sessionId;
  final int initialPingInterval;
  final VoidCallback? onEditMap;

  const LobbySettingsSheet({
    super.key,
    required this.sessionId,
    required this.initialPingInterval,
    this.onEditMap,
  });

  static Future<int?> show({
    required BuildContext context,
    required int sessionId,
    required int initialPingInterval,
    VoidCallback? onEditMap,
  }) {
    return showModalBottomSheet<int>(
      context: context,
      backgroundColor: XActColors.surface,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => LobbySettingsSheet(
        sessionId: sessionId,
        initialPingInterval: initialPingInterval,
        onEditMap: onEditMap,
      ),
    );
  }

  @override
  State<LobbySettingsSheet> createState() => _LobbySettingsSheetState();
}

class _LobbySettingsSheetState extends State<LobbySettingsSheet> {
  late int _interval;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _interval = widget.initialPingInterval.clamp(1, 30);
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      await ApiService.instance.updateSessionPingInterval(
        sessionId: widget.sessionId,
        mrXRevealInterval: _interval,
      );
      if (mounted) Navigator.of(context).pop(_interval);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Could not save settings: $e')));
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
            _PingIntervalCard(
              interval: _interval,
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

class _PingIntervalCard extends StatelessWidget {
  final int interval;
  final ValueChanged<int> onChanged;

  const _PingIntervalCard({required this.interval, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 4),
      decoration: BoxDecoration(
        color: XActColors.bg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Ping Interval', style: XActText.bodySm),
                  const SizedBox(height: 2),
                  Text(
                    'Mister X revealed every N minutes',
                    style: XActText.caption.copyWith(color: XActColors.text4),
                  ),
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: XActColors.secondary.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '$interval min',
                  style: XActText.bodySm.copyWith(
                    color: XActColors.secondary,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
          Slider(
            value: interval.toDouble(),
            min: 1,
            max: 30,
            divisions: 29,
            activeColor: XActColors.secondary,
            onChanged: (v) => onChanged(v.round()),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(10, 0, 10, 4),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '1 min',
                  style: XActText.caption.copyWith(color: XActColors.text4),
                ),
                Text(
                  '30 min',
                  style: XActText.caption.copyWith(color: XActColors.text4),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
