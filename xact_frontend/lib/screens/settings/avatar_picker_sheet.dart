import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/widgets/user_avatar.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

/// lets the player pick an emoji and a colour; returns true once saved
class AvatarPickerSheet extends StatefulWidget {
  const AvatarPickerSheet({super.key, required this.profile});

  final MyProfile profile;

  static Future<bool> show(BuildContext context, MyProfile profile) async {
    final saved = await showModalBottomSheet<bool>(
      context: context,
      backgroundColor: XActColors.surface,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => AvatarPickerSheet(profile: profile),
    );
    return saved ?? false;
  }

  @override
  State<AvatarPickerSheet> createState() => _AvatarPickerSheetState();
}

class _AvatarPickerSheetState extends State<AvatarPickerSheet> {
  static const _emojis = [
    '🕵️', '🦊', '🐺', '🦉', '🐱', '🐶', '🐼', '🐸', //
    '🦁', '🐯', '🐙', '🦄', '👻', '🤖', '👽', '🎩', //
    '🕶️', '🚲', '🛴', '🚇', '🗺️', '🔦', '⚡', '🔥', //
  ];

  // saturated enough to carry a white initial, picked to sit well on the
  // dark background
  static const _colors = [
    '#FF4D5E', '#5B7CFA', '#34D399', '#F6B05B', //
    '#A78BFA', '#F472B6', '#22D3EE', '#94A3B8', //
  ];

  late String? _emoji = widget.profile.avatarEmoji;
  late String? _color = widget.profile.avatarColor;
  bool _saving = false;
  String? _error;

  Future<void> _save() async {
    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      await ApiService.instance.updateMyProfile(
        username: widget.profile.username,
        avatarEmoji: _emoji,
        avatarColor: _color,
      );
      if (mounted) Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) {
        setState(() {
          _saving = false;
          _error = describeApiError(e);
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final error = _error;

    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(24, 20, 24, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            XActBranding.buildEyebrow('Profile'),
            const SizedBox(height: 4),
            Text('Your avatar', style: XActText.heading),
            const SizedBox(height: 20),
            Center(
              child: UserAvatar(
                name: widget.profile.username,
                emoji: _emoji,
                color: _color,
                size: 88,
                glow: true,
              ),
            ),
            const SizedBox(height: 24),
            Text('Emoji', style: XActText.bodySm),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildChoice(
                  selected: _emoji == null,
                  onTap: () => setState(() => _emoji = null),
                  child: Text(
                    UserAvatar.initialsOf(widget.profile.username),
                    style: XActText.bodySm.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                for (final emoji in _emojis)
                  _buildChoice(
                    selected: _emoji == emoji,
                    onTap: () => setState(() => _emoji = emoji),
                    child: Text(emoji, style: const TextStyle(fontSize: 22)),
                  ),
              ],
            ),
            const SizedBox(height: 20),
            Text('Colour', style: XActText.bodySm),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildChoice(
                  selected: _color == null,
                  onTap: () => setState(() => _color = null),
                  child: Container(
                    width: 28,
                    height: 28,
                    decoration: const BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: LinearGradient(
                        colors: [
                          XActColors.primaryLight,
                          XActColors.primaryDark,
                        ],
                      ),
                    ),
                  ),
                ),
                for (final color in _colors)
                  _buildChoice(
                    selected: _color == color,
                    onTap: () => setState(() => _color = color),
                    child: Container(
                      width: 28,
                      height: 28,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: UserAvatar.parseHexColor(color),
                      ),
                    ),
                  ),
              ],
            ),
            if (error != null) ...[
              const SizedBox(height: 16),
              Text(
                error,
                style: XActText.bodySm.copyWith(color: XActColors.primary),
              ),
            ],
            const SizedBox(height: 24),
            XActBranding.buildPrimaryButton(
              text: _saving ? 'Saving…' : 'Save avatar',
              height: 52,
              onPressed: _saving ? null : _save,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildChoice({
    required bool selected,
    required VoidCallback onTap,
    required Widget child,
  }) {
    return Material(
      color: selected ? XActColors.secondarySoft : XActColors.surface2,
      shape: RoundedRectangleBorder(
        borderRadius: XActRadius.md,
        side: BorderSide(
          color: selected ? XActColors.secondary : Colors.transparent,
          width: 1.5,
        ),
      ),
      child: InkWell(
        onTap: _saving ? null : onTap,
        borderRadius: XActRadius.md,
        child: SizedBox(width: 44, height: 44, child: Center(child: child)),
      ),
    );
  }
}
