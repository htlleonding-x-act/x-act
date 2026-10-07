import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

/// the chosen emoji on the chosen colour, or the initials of [name] when the
/// player hasn't picked an emoji yet
class UserAvatar extends StatelessWidget {
  const UserAvatar({
    super.key,
    required this.name,
    this.emoji,
    this.color,
    this.size = 40,
    this.glow = false,
  });

  final String name;
  final String? emoji;

  /// hex colour like `#5B7CFA`; null keeps the red brand gradient
  final String? color;
  final double size;
  final bool glow;

  @override
  Widget build(BuildContext context) {
    final emoji = this.emoji;
    final background = parseHexColor(color);

    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: background,
        gradient: background == null
            ? const LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [XActColors.primaryLight, XActColors.primaryDark],
              )
            : null,
        boxShadow: glow
            ? [
                BoxShadow(
                  color: (background ?? XActColors.primary).withValues(
                    alpha: .35,
                  ),
                  blurRadius: 24,
                ),
              ]
            : XActElevation.e1,
        border: Border.all(
          color: Colors.white.withValues(alpha: .12),
          width: size > 60 ? 2 : 1,
        ),
      ),
      child: Center(
        child: emoji != null
            ? Text(emoji, style: TextStyle(fontSize: size * .5))
            : Text(
                initialsOf(name),
                style: GoogleFonts.spaceGrotesk(
                  fontSize: size * .34,
                  fontWeight: FontWeight.w700,
                  color: Colors.white,
                  letterSpacing: -1,
                ),
              ),
      ),
    );
  }

  static Color? parseHexColor(String? hex) {
    if (hex == null || hex.length != 7) return null;
    final value = int.tryParse(hex.substring(1), radix: 16);
    return value == null ? null : Color(0xFF000000 | value);
  }

  static String initialsOf(String name) {
    final trimmed = name.trim();
    if (trimmed.isEmpty) return 'P';

    // characters, not code units, so a name starting with an emoji keeps it
    // whole instead of showing half of it
    final parts = trimmed.split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return '${parts.first.characters.first}${parts.last.characters.first}'
          .toUpperCase();
    }
    return trimmed.characters.take(2).toString().toUpperCase();
  }
}
