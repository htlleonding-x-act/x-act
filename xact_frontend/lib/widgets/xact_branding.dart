import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/api/models.dart';

// ───────────────────────────── COLORS ───────────────────────────────
class XActColors {
  XActColors._();

  // Surface ramp — deeper, cooler navy
  static const Color bg = Color(0xFF0A0E1A);
  static const Color bg2 = Color(0xFF0F1422);
  static const Color surface = Color(0xFF161C2E);
  static const Color surface2 = Color(0xFF1E2640);
  static const Color surface3 = Color(0xFF2A3354);

  // Legacy aliases (kept for back-compat at call sites)
  static const Color surfaceAlt = surface2;
  static const Color surfaceDeep = bg2;
  static const Color hairline = surface3;

  static final Color glass = const Color(0xFF161C2E).withValues(alpha: .72);
  static final Color glassHi = const Color(0xFF283250).withValues(alpha: .55);

  // Brand & semantic
  static const Color primary = Color(0xFFFF4D5E);
  static const Color primaryLight = Color(0xFFFF6173);
  static const Color primaryDark = Color(0xFFE83847);
  static const Color secondary = Color(0xFF5B7CFA);
  static const Color secondaryLight = Color(0xFF6B8AFB);
  static const Color secondaryDark = Color(0xFF4865E8);
  static const Color success = Color(0xFF34D399);
  static const Color successDark = Color(0xFF21B884);
  static const Color warning = Color(0xFFF6B05B);
  static const Color danger = primary;

  // Role
  static const Color roleMrX = primary;
  static const Color roleDetective = secondary;
  static const Color roleSpectator = Color(0xFF94A3B8);

  // Player location accent (used on map)
  static const Color youLocation = secondary;

  // Soft / glow tints
  static final Color primarySoft = primary.withValues(alpha: .16);
  static final Color primaryGlow = primary.withValues(alpha: .35);
  static final Color secondarySoft = secondary.withValues(alpha: .18);
  static final Color secondaryGlow = secondary.withValues(alpha: .40);
  static final Color successSoft = success.withValues(alpha: .16);
  static final Color spectatorSoft = roleSpectator.withValues(alpha: .16);

  // Text scale (5 alphas)
  static const Color text1 = Colors.white;
  static final Color text2 = Colors.white.withValues(alpha: .78);
  static final Color text3 = Colors.white.withValues(alpha: .55);
  static final Color text4 = Colors.white.withValues(alpha: .36);
  static final Color text5 = Colors.white.withValues(alpha: .22);

  // Hairlines
  static final Color hairlineSoft = Colors.white.withValues(alpha: .10);
  static final Color hairlineFaint = Colors.white.withValues(alpha: .06);
  static final Color hairlineHi = Colors.white.withValues(alpha: .18);

  static Color roleColor(TeamRole? role) => switch (role) {
        TeamRole.mrX => roleMrX,
        TeamRole.detective => roleDetective,
        TeamRole.spectator => roleSpectator,
        null => roleSpectator,
      };

  static Color roleSoft(TeamRole? role) => switch (role) {
        TeamRole.mrX => primarySoft,
        TeamRole.detective => secondarySoft,
        TeamRole.spectator => spectatorSoft,
        null => spectatorSoft,
      };
}

// ───────────────────────────── GEOMETRY ─────────────────────────────
class XActSpace {
  XActSpace._();
  static const double s1 = 4;
  static const double s2 = 8;
  static const double s3 = 12;
  static const double s4 = 16;
  static const double s5 = 20;
  static const double s6 = 24;
  static const double s7 = 32;
  static const double s8 = 40;
  static const double s9 = 56;
}

class XActRadius {
  XActRadius._();
  static const BorderRadius xs = BorderRadius.all(Radius.circular(8));
  static const BorderRadius sm = BorderRadius.all(Radius.circular(12));
  static const BorderRadius md = BorderRadius.all(Radius.circular(16));
  static const BorderRadius lg = BorderRadius.all(Radius.circular(20));
  static const BorderRadius xl = BorderRadius.all(Radius.circular(28));
  static const BorderRadius xxl = BorderRadius.all(Radius.circular(36));
  static const BorderRadius pill = BorderRadius.all(Radius.circular(9999));
}

// ───────────────────────────── ELEVATION ────────────────────────────
class XActElevation {
  XActElevation._();

  static List<BoxShadow> e1 = [
    BoxShadow(
      color: Colors.black.withValues(alpha: .35),
      blurRadius: 6,
      offset: const Offset(0, 2),
    ),
  ];

  static List<BoxShadow> e2 = [
    BoxShadow(
      color: Colors.black.withValues(alpha: .40),
      blurRadius: 24,
      offset: const Offset(0, 8),
    ),
  ];

  static List<BoxShadow> e3 = [
    BoxShadow(
      color: Colors.black.withValues(alpha: .55),
      blurRadius: 50,
      offset: const Offset(0, 20),
    ),
  ];

  static List<BoxShadow> glowRed = [
    BoxShadow(
      color: XActColors.primary.withValues(alpha: .35),
      blurRadius: 32,
      offset: const Offset(0, 12),
    ),
  ];

  static List<BoxShadow> glowBlue = [
    BoxShadow(
      color: XActColors.secondary.withValues(alpha: .35),
      blurRadius: 32,
      offset: const Offset(0, 12),
    ),
  ];

  static List<BoxShadow> glowGreen = [
    BoxShadow(
      color: XActColors.success.withValues(alpha: .35),
      blurRadius: 32,
      offset: const Offset(0, 12),
    ),
  ];
}

// ───────────────────────────── TYPE ─────────────────────────────────
class XActText {
  XActText._();

  // Display / wordmark (Space Grotesk)
  static TextStyle display = GoogleFonts.spaceGrotesk(
    fontSize: 56,
    fontWeight: FontWeight.w700,
    color: XActColors.text1,
    letterSpacing: -2.2,
    height: 1,
  );

  static TextStyle displaySm = GoogleFonts.spaceGrotesk(
    fontSize: 32,
    fontWeight: FontWeight.w700,
    color: XActColors.text1,
    letterSpacing: -.6,
  );

  // UI titles (Inter)
  static TextStyle title = GoogleFonts.inter(
    fontSize: 22,
    fontWeight: FontWeight.w700,
    color: XActColors.text1,
    letterSpacing: -.2,
  );

  static TextStyle heading = GoogleFonts.inter(
    fontSize: 18,
    fontWeight: FontWeight.w700,
    color: XActColors.text1,
    letterSpacing: -.1,
  );

  static TextStyle subheading = GoogleFonts.inter(
    fontSize: 17,
    fontWeight: FontWeight.w600,
    color: XActColors.text1,
  );

  static TextStyle body = GoogleFonts.inter(
    fontSize: 16,
    color: XActColors.text1,
    fontWeight: FontWeight.w400,
    height: 1.5,
  );

  static TextStyle bodySm = GoogleFonts.inter(
    fontSize: 14,
    color: XActColors.text1,
    fontWeight: FontWeight.w500,
  );

  static TextStyle caption = GoogleFonts.inter(
    fontSize: 12,
    fontWeight: FontWeight.w500,
    color: XActColors.text3,
  );

  // Eyebrow / overline (mono uppercase)
  static TextStyle eyebrow = GoogleFonts.jetBrainsMono(
    fontSize: 11,
    fontWeight: FontWeight.w600,
    color: XActColors.text3,
    letterSpacing: 1.6,
  );

  // Mono — codes & timers
  static TextStyle mono = GoogleFonts.jetBrainsMono(
    fontSize: 22,
    fontWeight: FontWeight.w600,
    color: XActColors.text1,
    letterSpacing: 1,
  );

  static TextStyle codeXl = GoogleFonts.jetBrainsMono(
    fontSize: 38,
    fontWeight: FontWeight.w600,
    color: XActColors.text1,
    letterSpacing: 4.6,
  );
}

// ───────────────────────────── BRANDING ─────────────────────────────
class XActBranding {
  XActBranding._();

  // Re-exported for back-compat with current call sites.
  static const Color primaryRed = XActColors.primary;
  static const Color primaryBlue = XActColors.secondary;
  static const Color backgroundColor = XActColors.bg;
  static const Color cardColor = XActColors.surface;

  /// Auroral gradient backdrop — placed behind hero content on dark screens.
  static Widget aurora({Widget? child}) {
    return Stack(
      children: [
        Positioned(
          top: -120,
          right: -100,
          child: _glowBlob(
            color: XActColors.primary.withValues(alpha: .35),
            size: 320,
          ),
        ),
        Positioned(
          bottom: -80,
          left: -80,
          child: _glowBlob(
            color: XActColors.secondary.withValues(alpha: .40),
            size: 280,
          ),
        ),
        ?child,
      ],
    );
  }

  static Widget _glowBlob({required Color color, required double size}) {
    return IgnorePointer(
      child: Container(
        width: size,
        height: size,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          gradient: RadialGradient(
            colors: [color, color.withValues(alpha: 0)],
            stops: const [0, 1],
          ),
        ),
      ),
    );
  }

  /// Square logo mark with the "X" glyph and a red gradient.
  static Widget buildLogo({double size = 88}) {
    final radius = size * .30;
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(radius),
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [XActColors.primaryLight, Color(0xFFB82431)],
        ),
        boxShadow: [
          BoxShadow(
            color: XActColors.primary.withValues(alpha: .45),
            blurRadius: 40,
            offset: const Offset(0, 16),
          ),
          BoxShadow(
            color: XActColors.primary.withValues(alpha: .10),
            blurRadius: 0,
            spreadRadius: 6,
          ),
        ],
        border: Border.all(
          color: Colors.white.withValues(alpha: .08),
          width: 1,
        ),
      ),
      child: Center(
        child: Text(
          'X',
          style: GoogleFonts.spaceGrotesk(
            fontSize: size * .5,
            fontWeight: FontWeight.w700,
            color: Colors.white,
            letterSpacing: -size * .03,
          ),
        ),
      ),
    );
  }

  /// Header used on the start screen — logo + wordmark + tagline + body copy.
  static Widget buildHeader({bool compact = false}) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        buildLogo(size: compact ? 80 : 104),
        SizedBox(height: compact ? XActSpace.s4 : XActSpace.s6),
        Text(
          'x-act',
          style: XActText.display.copyWith(
            fontSize: compact ? 40 : 56,
            letterSpacing: compact ? -1.4 : -2.2,
          ),
        ),
        const SizedBox(height: XActSpace.s2),
        Text(
          'REAL-TIME CHASE',
          style: GoogleFonts.inter(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: XActColors.text3,
            letterSpacing: 2.4,
          ),
        ),
        if (!compact) ...[
          const SizedBox(height: XActSpace.s5),
          ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 280),
            child: Text(
              'Hunt down Mister X across the streets, or vanish into the city as the phantom.',
              textAlign: TextAlign.center,
              style: XActText.body.copyWith(color: XActColors.text2),
            ),
          ),
        ],
      ],
    );
  }

  static Widget buildFooter() {
    return Text(
      'Play responsibly · Stay safe · Follow local laws',
      style: XActText.caption.copyWith(color: XActColors.text4),
    );
  }

  // ─── Buttons ────────────────────────────────────────────────────────

  static Widget buildPrimaryButton({
    required String text,
    required VoidCallback? onPressed,
    IconData? icon,
    double height = 60,
  }) {
    return _gradientButton(
      text: text,
      icon: icon,
      onPressed: onPressed,
      height: height,
      fontSize: 17,
      fontWeight: FontWeight.w700,
      gradient: const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [XActColors.primaryLight, XActColors.primaryDark],
      ),
      glow: XActElevation.glowRed,
    );
  }

  static Widget buildSecondaryButton({
    required String text,
    required VoidCallback? onPressed,
    IconData? icon,
    double height = 56,
  }) {
    return _gradientButton(
      text: text,
      icon: icon,
      onPressed: onPressed,
      height: height,
      fontSize: 16,
      fontWeight: FontWeight.w600,
      gradient: const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [XActColors.secondaryLight, XActColors.secondaryDark],
      ),
      glow: XActElevation.glowBlue,
    );
  }

  static Widget buildSuccessButton({
    required String text,
    IconData? icon,
    required VoidCallback? onPressed,
    double height = 56,
  }) {
    return _gradientButton(
      text: text,
      icon: icon,
      onPressed: onPressed,
      height: height,
      fontSize: 16,
      fontWeight: FontWeight.w700,
      gradient: const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Color(0xFF4FE3AF), XActColors.successDark],
      ),
      glow: XActElevation.glowGreen,
    );
  }

  static Widget buildGhostButton({
    required String text,
    required VoidCallback? onPressed,
    IconData? icon,
    Widget? trailing,
    double height = 52,
    Color? foreground,
  }) {
    final enabled = onPressed != null;
    final fg = foreground ?? XActColors.text2;
    return SizedBox(
      width: double.infinity,
      height: height,
      child: Material(
        color: Colors.white.withValues(alpha: enabled ? .04 : .02),
        borderRadius: XActRadius.md,
        child: InkWell(
          onTap: onPressed,
          borderRadius: XActRadius.md,
          child: Ink(
            decoration: BoxDecoration(
              borderRadius: XActRadius.md,
              border: Border.all(color: XActColors.hairlineSoft),
            ),
            child: Center(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (icon != null) ...[
                    Icon(icon, size: 18, color: fg),
                    const SizedBox(width: 10),
                  ],
                  Text(
                    text,
                    style: XActText.body.copyWith(
                      color: fg,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  if (trailing != null) ...[
                    const SizedBox(width: 10),
                    trailing,
                  ],
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  static Widget buildCancelButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 56,
  }) {
    return buildGhostButton(
      text: text,
      onPressed: onPressed,
      height: height,
      foreground: XActColors.text1,
    );
  }

  static Widget _gradientButton({
    required String text,
    required IconData? icon,
    required VoidCallback? onPressed,
    required double height,
    required double fontSize,
    required FontWeight fontWeight,
    required Gradient gradient,
    required List<BoxShadow> glow,
  }) {
    final enabled = onPressed != null;
    return SizedBox(
      width: double.infinity,
      height: height,
      child: Material(
        color: Colors.transparent,
        borderRadius: XActRadius.md,
        child: InkWell(
          onTap: onPressed,
          borderRadius: XActRadius.md,
          child: Ink(
            decoration: BoxDecoration(
              borderRadius: XActRadius.md,
              gradient: enabled
                  ? gradient
                  : LinearGradient(
                      colors: [
                        Colors.white.withValues(alpha: .05),
                        Colors.white.withValues(alpha: .05),
                      ],
                    ),
              boxShadow: enabled ? glow : null,
              border: Border.all(
                color: Colors.white.withValues(alpha: enabled ? .18 : .04),
                width: 1,
              ),
            ),
            child: Center(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (icon != null) ...[
                    Icon(
                      icon,
                      size: fontSize + 4,
                      color: enabled ? Colors.white : XActColors.text4,
                    ),
                    const SizedBox(width: 10),
                  ],
                  Text(
                    text,
                    style: GoogleFonts.inter(
                      fontSize: fontSize,
                      fontWeight: fontWeight,
                      color: enabled ? Colors.white : XActColors.text4,
                      letterSpacing: -.1,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  // ─── Cards / forms ────────────────────────────────────────────────

  static Widget buildFormCard({required Widget child}) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(XActSpace.s5),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        boxShadow: XActElevation.e2,
        border: Border.all(color: XActColors.hairlineSoft),
      ),
      child: child,
    );
  }

  /// Eyebrow label (mono uppercase, used for section headers).
  static Widget buildEyebrow(String text, {Color? color}) {
    return Text(
      text.toUpperCase(),
      style: XActText.eyebrow.copyWith(color: color ?? XActColors.text3),
    );
  }

  /// Top app bar row with optional back chevron and trailing widget.
  static Widget buildTopBar({
    BuildContext? context,
    String? eyebrow,
    String? title,
    bool showBack = true,
    Widget? trailing,
    VoidCallback? onBack,
  }) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        XActSpace.s4,
        XActSpace.s2,
        XActSpace.s4,
        XActSpace.s4,
      ),
      child: Row(
        children: [
          if (showBack && context != null)
            _circleIconButton(
              icon: Icons.arrow_back_rounded,
              onPressed: onBack ?? () => Navigator.of(context).maybePop(),
            ),
          if (showBack && context != null) const SizedBox(width: XActSpace.s3),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (eyebrow != null)
                  buildEyebrow(eyebrow),
                if (eyebrow != null && title != null)
                  const SizedBox(height: 2),
                if (title != null)
                  Text(title, style: XActText.heading),
              ],
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }

  static Widget _circleIconButton({
    required IconData icon,
    required VoidCallback onPressed,
    double size = 40,
  }) {
    return Material(
      color: Colors.white.withValues(alpha: .04),
      shape: const CircleBorder(),
      child: InkWell(
        onTap: onPressed,
        customBorder: const CircleBorder(),
        child: Ink(
          width: size,
          height: size,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            border: Border.all(color: XActColors.hairlineSoft),
          ),
          child: Icon(icon, size: 20, color: XActColors.text1),
        ),
      ),
    );
  }

  static Widget circleIconButton({
    required IconData icon,
    required VoidCallback onPressed,
    double size = 40,
  }) => _circleIconButton(icon: icon, onPressed: onPressed, size: size);

  /// Reusable horizontal action card.
  static Widget buildActionCard({
    required IconData icon,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
    Color bg = XActColors.surface,
  }) {
    return Material(
      color: bg,
      borderRadius: XActRadius.md,
      child: InkWell(
        borderRadius: XActRadius.md,
        onTap: onTap,
        child: Ink(
          decoration: BoxDecoration(
            borderRadius: XActRadius.md,
            border: Border.all(color: XActColors.hairlineSoft),
            boxShadow: XActElevation.e1,
          ),
          child: Padding(
            padding: const EdgeInsets.all(XActSpace.s5),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Container(
                  width: 48,
                  height: 48,
                  decoration: BoxDecoration(
                    color: XActColors.surface3,
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: Icon(icon, color: XActColors.text1, size: 22),
                ),
                const SizedBox(width: XActSpace.s4),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(title, style: XActText.subheading),
                      const SizedBox(height: 2),
                      Text(
                        subtitle,
                        style: XActText.caption.copyWith(
                          color: XActColors.text3,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_right_rounded,
                  color: XActColors.text4,
                  size: 22,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  // ─── Inputs ───────────────────────────────────────────────────────

  static Widget buildTextField({
    required String label,
    required String hintText,
    required TextEditingController controller,
    TextInputType? keyboardType,
    int? maxLength,
    List<TextInputFormatter>? inputFormatters,
    TextCapitalization textCapitalization = TextCapitalization.none,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: XActText.bodySm.copyWith(
            color: XActColors.text3,
            fontWeight: FontWeight.w500,
            letterSpacing: .2,
          ),
        ),
        const SizedBox(height: XActSpace.s2),
        TextField(
          controller: controller,
          keyboardType: keyboardType,
          maxLength: maxLength,
          inputFormatters: inputFormatters,
          textCapitalization: textCapitalization,
          style: XActText.subheading,
          cursorColor: XActColors.secondary,
          decoration: InputDecoration(
            hintText: hintText,
            hintStyle: XActText.subheading.copyWith(color: XActColors.text4),
            filled: true,
            fillColor: Colors.white.withValues(alpha: .03),
            counterText: '',
            border: OutlineInputBorder(
              borderRadius: XActRadius.md,
              borderSide: BorderSide(color: XActColors.hairlineSoft),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: XActRadius.md,
              borderSide: BorderSide(color: XActColors.hairlineSoft),
            ),
            focusedBorder: const OutlineInputBorder(
              borderRadius: XActRadius.md,
              borderSide: BorderSide(
                color: XActColors.secondary,
                width: 2,
              ),
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 18,
              vertical: 16,
            ),
          ),
        ),
      ],
    );
  }

  // ─── Role pill ────────────────────────────────────────────────────

  static Widget buildRolePill({
    required TeamRole? role,
    String? overrideLabel,
  }) {
    final color = XActColors.roleColor(role);
    final bg = XActColors.roleSoft(role);
    final label = overrideLabel ??
        switch (role) {
          TeamRole.mrX => 'MISTER X',
          TeamRole.detective => 'DETECTIVE',
          TeamRole.spectator => 'SPECTATOR',
          null => 'PLAYER',
        };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: XActRadius.pill,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 6,
            height: 6,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: color,
              boxShadow: [
                BoxShadow(
                  color: color.withValues(alpha: .6),
                  blurRadius: 6,
                ),
              ],
            ),
          ),
          const SizedBox(width: 6),
          Text(
            label,
            style: GoogleFonts.inter(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              color: color,
              letterSpacing: 1.1,
            ),
          ),
        ],
      ),
    );
  }
}
