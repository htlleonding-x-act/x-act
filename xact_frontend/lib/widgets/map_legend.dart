import 'package:flutter/material.dart';

import 'xact_branding.dart';

final class MapLegendTeamEntry {
  final String label;
  final Color color;

  const MapLegendTeamEntry({required this.label, required this.color});
}

class MapLegend extends StatelessWidget {
  final List<MapLegendTeamEntry> teamEntries;
  final Color myLocationColor;

  const MapLegend({
    super.key,
    required this.teamEntries,
    required this.myLocationColor,
  });

  @override
  Widget build(BuildContext context) {
    final items = <Widget>[
      _LegendItem(color: myLocationColor, label: 'You'),
    ];

    for (final entry in teamEntries) {
      items.add(const SizedBox(height: 8));
      items.add(_LegendItem(color: entry.color, label: entry.label));
    }

    return Positioned(
      bottom: 16,
      left: 16,
      child: Container(
        padding: const EdgeInsets.fromLTRB(14, 12, 16, 12),
        decoration: BoxDecoration(
          color: XActColors.glass,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: XActColors.hairlineSoft),
          boxShadow: XActElevation.e2,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            XActBranding.buildEyebrow('Legend'),
            const SizedBox(height: 8),
            ...items,
          ],
        ),
      ),
    );
  }
}

class _LegendItem extends StatelessWidget {
  final Color color;
  final String label;

  const _LegendItem({required this.color, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 12,
          height: 12,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: color,
            boxShadow: [
              BoxShadow(
                color: color.withValues(alpha: .55),
                blurRadius: 8,
              ),
            ],
          ),
        ),
        const SizedBox(width: 10),
        _ScrollingLabel(
          text: label,
          style: XActText.bodySm.copyWith(fontSize: 13),
        ),
      ],
    );
  }
}

/// team names can be 50 characters long, so a name that doesn't fit the
/// legend scrolls through instead of stretching the legend over the map
class _ScrollingLabel extends StatefulWidget {
  final String text;
  final TextStyle style;

  const _ScrollingLabel({required this.text, required this.style});

  @override
  State<_ScrollingLabel> createState() => _ScrollingLabelState();
}

class _ScrollingLabelState extends State<_ScrollingLabel> {
  static const double _maxWidth = 150;
  static const Duration _pause = Duration(milliseconds: 1500);

  final ScrollController _controller = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollLoop();
  }

  Future<void> _scrollLoop() async {
    while (mounted) {
      await Future<void>.delayed(_pause);
      if (!mounted || !_controller.hasClients) continue;

      final overflow = _controller.position.maxScrollExtent;
      if (overflow <= 0) continue;

      await _controller.animateTo(
        overflow,
        duration: Duration(milliseconds: (overflow * 30).round()),
        curve: Curves.linear,
      );
      await Future<void>.delayed(_pause);
      if (mounted && _controller.hasClients) _controller.jumpTo(0);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: _maxWidth),
      child: SingleChildScrollView(
        controller: _controller,
        scrollDirection: Axis.horizontal,
        physics: const NeverScrollableScrollPhysics(),
        child: Text(widget.text, style: widget.style, softWrap: false),
      ),
    );
  }
}
