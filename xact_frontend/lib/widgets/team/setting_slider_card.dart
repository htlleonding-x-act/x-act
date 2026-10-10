import 'package:flutter/material.dart';

import '../xact_branding.dart';

/// a game setting in minutes, picked with a slider
class SettingSliderCard extends StatelessWidget {
  final String title;
  final String caption;
  final int minutes;
  final int min;
  final int max;
  final int step;
  final ValueChanged<int> onChanged;

  const SettingSliderCard({
    super.key,
    required this.title,
    required this.caption,
    required this.minutes,
    required this.min,
    required this.max,
    this.step = 1,
    required this.onChanged,
  });

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
                  Text(title, style: XActText.bodySm),
                  const SizedBox(height: 2),
                  Text(
                    caption,
                    style: XActText.caption.copyWith(color: XActColors.text4),
                  ),
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: XActColors.secondary.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '$minutes min',
                  style: XActText.bodySm.copyWith(
                    color: XActColors.secondary,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            ],
          ),
          Slider(
            value: minutes.toDouble(),
            min: min.toDouble(),
            max: max.toDouble(),
            divisions: (max - min) ~/ step,
            activeColor: XActColors.secondary,
            onChanged: (v) => onChanged(v.round()),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(10, 0, 10, 4),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '$min min',
                  style: XActText.caption.copyWith(color: XActColors.text4),
                ),
                Text(
                  '$max min',
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

/// the bounds match the backend validation of the planned duration
class MatchLengthCard extends StatelessWidget {
  static const int minMinutes = 10;
  static const int maxMinutes = 180;
  static const int defaultMinutes = 60;

  final int minutes;
  final ValueChanged<int> onChanged;

  const MatchLengthCard({
    super.key,
    required this.minutes,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return SettingSliderCard(
      title: 'Match Length',
      caption: 'Mister X wins when the time runs out',
      minutes: minutes,
      min: minMinutes,
      max: maxMinutes,
      step: 5,
      onChanged: onChanged,
    );
  }
}
