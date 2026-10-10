import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

/// icons a player can pick as avatar, by the key the backend stores.
/// single-colour so they can sit on any team colour, and part of the bundled
/// icon font so they look the same on every device and never load late
const Map<String, IconData> avatarIcons = {
  'magnifier': Icons.search_rounded,
  'fingerprint': Icons.fingerprint_rounded,
  'eye': Icons.visibility_rounded,
  'masks': Icons.theater_comedy_rounded,
  'police': Icons.local_police_rounded,
  'key': Icons.key_rounded,
  'compass': Icons.explore_rounded,
  'radar': Icons.radar_rounded,
  'satellite': Icons.satellite_alt_rounded,
  'map': Icons.map_rounded,
  'rocket': Icons.rocket_launch_rounded,
  'bolt': Icons.bolt_rounded,
  'paw': Icons.pets_rounded,
  'rabbit': Icons.cruelty_free_rounded,
  'mouse': Icons.pest_control_rodent_rounded,
  'bird': Icons.flutter_dash_rounded,
  'bee': Icons.emoji_nature_rounded,
  'bug': Icons.bug_report_rounded,
  'flame': Icons.local_fire_department_rounded,
  'moon': Icons.nightlight_rounded,
  'snowflake': Icons.ac_unit_rounded,
  'diamond': Icons.diamond_rounded,
  'runner': Icons.directions_run_rounded,
  'bike': Icons.directions_bike_rounded,
};

/// the chosen icon, or the initials of [name] when the player hasn't picked
/// one or picked one this app version doesn't know
class UserAvatar extends StatelessWidget {
  const UserAvatar({
    super.key,
    required this.name,
    this.icon,
    this.size = 40,
    this.glow = false,
  });

  final String name;

  /// key in [avatarIcons]
  final String? icon;
  final double size;
  final bool glow;

  @override
  Widget build(BuildContext context) {
    final iconData = avatarIcons[icon];

    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [XActColors.primaryLight, XActColors.primaryDark],
        ),
        boxShadow: glow
            ? [
                BoxShadow(
                  color: XActColors.primary.withValues(alpha: .35),
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
        child: iconData != null
            ? Icon(iconData, size: size * .5, color: Colors.white)
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
