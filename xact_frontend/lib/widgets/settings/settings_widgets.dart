import 'package:flutter/material.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class SettingsSection extends StatelessWidget {
  const SettingsSection({
    super.key,
    required this.label,
    required this.children,
    this.borderColor,
  });

  final String label;
  final List<Widget> children;
  final Color? borderColor;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: XActBranding.buildEyebrow(label),
        ),
        Container(
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            color: XActColors.surface,
            borderRadius: XActRadius.lg,
            border: Border.all(color: borderColor ?? XActColors.hairlineSoft),
            boxShadow: XActElevation.e1,
          ),
          child: Material(
            type: MaterialType.transparency,
            child: Column(children: children),
          ),
        ),
      ],
    );
  }
}

class SettingsInfoRow extends StatelessWidget {
  const SettingsInfoRow({
    super.key,
    required this.icon,
    required this.title,
    required this.value,
  });

  final IconData icon;
  final String title;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(
        children: [
          Icon(icon, size: 20, color: XActColors.text3),
          const SizedBox(width: 14),
          Text(title, style: XActText.bodySm),
          const SizedBox(width: 14),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: XActText.bodySm.copyWith(color: XActColors.text3),
            ),
          ),
        ],
      ),
    );
  }
}

/// a tappable row; [color] tints icon and title, e.g. red for destructive actions
class SettingsTile extends StatelessWidget {
  const SettingsTile({
    super.key,
    required this.icon,
    required this.title,
    required this.onTap,
    this.subtitle,
    this.trailingIcon = Icons.chevron_right_rounded,
    this.color,
  });

  final IconData icon;
  final String title;
  final String? subtitle;
  final VoidCallback? onTap;
  final IconData trailingIcon;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final subtitle = this.subtitle;

    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Row(
          children: [
            Icon(icon, size: 20, color: color ?? XActColors.text3),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: XActText.bodySm.copyWith(
                      color: color,
                      fontWeight: color == null ? null : FontWeight.w600,
                    ),
                  ),
                  if (subtitle != null)
                    Text(
                      subtitle,
                      style: XActText.caption.copyWith(fontSize: 12),
                    ),
                ],
              ),
            ),
            Icon(trailingIcon, size: 18, color: XActColors.text5),
          ],
        ),
      ),
    );
  }
}

class SettingsDivider extends StatelessWidget {
  const SettingsDivider({super.key});

  @override
  Widget build(BuildContext context) {
    return Divider(
      height: 1,
      thickness: 1,
      color: XActColors.hairlineFaint,
      indent: 50,
    );
  }
}
