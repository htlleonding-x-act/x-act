import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:xact_frontend/api/models.dart';

// ───────────────────────────── COLORS ───────────────────────────────
class XActColors {
  XActColors._();

  // Surface
  static const Color bg = Color(0xFF1A1F2E);
  static const Color surface = Color(0xFF252A3A);
  static const Color surfaceAlt = Color(0xFF1E293B);
  static const Color surfaceDeep = Color(0xFF0F172A);
  static const Color hairline = Color(0xFF334155);

  // Brand & semantic
  static const Color primary = Color(0xFFE53935);
  static const Color secondary = Color(0xFF3D5AFE);
  static const Color success = Color(0xFF16A34A);
  static const Color danger = primary;

  // Role
  static const Color roleMrX = primary;
  static const Color roleDetective = secondary;
  static const Color roleSpectator = Color(0xFF94A3B8);

  // Player location accent (used on map)
  static const Color youLocation = Color(0xFF22D3EE);

  // Text (semantic alphas, replaces Colors.whiteNN sprawl)
  static const Color text1 = Colors.white;
  static final Color text2 = Colors.white.withValues(alpha: .70);
  static final Color text3 = Colors.white.withValues(alpha: .54);
  static final Color text4 = Colors.white.withValues(alpha: .38);
  static final Color text5 = Colors.white.withValues(alpha: .24);
  static final Color hairlineSoft = Colors.white.withValues(alpha: .15);

  static Color roleColor(TeamRole? role) => switch (role) {
        TeamRole.mrX => roleMrX,
        TeamRole.detective => roleDetective,
        TeamRole.spectator => roleSpectator,
        null => roleSpectator,
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
}

class XActRadius {
  XActRadius._();
  static const BorderRadius sm = BorderRadius.all(Radius.circular(8));
  static const BorderRadius md = BorderRadius.all(Radius.circular(12));
  static const BorderRadius lg = BorderRadius.all(Radius.circular(16));
  static const BorderRadius pill = BorderRadius.all(Radius.circular(9999));
}

// ───────────────────────────── TYPE ─────────────────────────────────
class XActText {
  XActText._();
  static const TextStyle display = TextStyle(
    fontSize: 48,
    fontWeight: FontWeight.bold,
    color: XActColors.text1,
    letterSpacing: 2,
  );
  static const TextStyle title = TextStyle(
    fontSize: 28,
    fontWeight: FontWeight.bold,
    color: XActColors.text1,
    letterSpacing: 4,
  );
  static const TextStyle heading = TextStyle(
    fontSize: 20,
    fontWeight: FontWeight.bold,
    color: XActColors.text1,
  );
  static const TextStyle subheading = TextStyle(
    fontSize: 18,
    fontWeight: FontWeight.w600,
    color: XActColors.text1,
  );
  static const TextStyle body = TextStyle(
    fontSize: 16,
    color: XActColors.text1,
  );
  static const TextStyle bodySm = TextStyle(
    fontSize: 14,
    color: XActColors.text1,
  );
  static const TextStyle caption = TextStyle(fontSize: 12);
}

// ───────────────────────────── BRANDING ─────────────────────────────
class XActBranding {
  XActBranding._();

  // Re-exported for back-compat with current call sites.
  static const Color primaryRed = XActColors.primary;
  static const Color primaryBlue = XActColors.secondary;
  static const Color backgroundColor = XActColors.bg;
  static const Color cardColor = XActColors.surface;

  static Widget buildLogo({double size = 80}) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: XActColors.primary, width: 3),
      ),
      child: Center(
        child: Icon(
          Icons.location_on,
          color: XActColors.primary,
          size: size * 0.5,
        ),
      ),
    );
  }

  static Widget buildHeader({bool compact = false}) {
    return Column(
      children: [
        buildLogo(),
        const SizedBox(height: XActSpace.s4),
        const Text('X-ACT', style: XActText.display),
        const SizedBox(height: XActSpace.s2),
        Text(
          'Real-Time Chase Game',
          style: XActText.subheading.copyWith(
            color: XActColors.text2,
            letterSpacing: 1,
          ),
        ),
        const SizedBox(height: XActSpace.s3),
        Text(
          'Outsmart Mister X across the city',
          style: XActText.bodySm.copyWith(color: XActColors.text3),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }

  static Widget buildFooter() {
    return Text(
      'Play responsibly • Stay safe • Follow local laws',
      style: XActText.caption.copyWith(color: XActColors.text4),
    );
  }

  // ─── Buttons ────────────────────────────────────────────────────────
  static Widget buildPrimaryButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 64,
  }) {
    return _filled(
      text,
      onPressed,
      XActColors.primary,
      height,
      XActText.heading.copyWith(fontSize: 20),
    );
  }

  static Widget buildSecondaryButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 56,
  }) {
    return _filled(
      text,
      onPressed,
      XActColors.secondary,
      height,
      XActText.body.copyWith(fontWeight: FontWeight.w600),
    );
  }

  static Widget buildCancelButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 56,
  }) {
    return _filled(
      text,
      onPressed,
      XActColors.surface,
      height,
      XActText.body.copyWith(fontWeight: FontWeight.w600),
    );
  }

  static Widget buildSuccessButton({
    required String text,
    IconData? icon,
    required VoidCallback? onPressed,
    double height = 48,
  }) {
    final enabled = onPressed != null;
    return SizedBox(
      width: double.infinity,
      height: height,
      child: ElevatedButton.icon(
        onPressed: onPressed,
        icon: icon != null
            ? Icon(icon, size: 20)
            : const SizedBox.shrink(),
        label: Text(
          text,
          style: XActText.body.copyWith(fontWeight: FontWeight.w600),
        ),
        style: ElevatedButton.styleFrom(
          backgroundColor: enabled
              ? XActColors.success
              : Colors.white.withValues(alpha: .06),
          foregroundColor: enabled ? XActColors.text1 : XActColors.text4,
          shape: const RoundedRectangleBorder(borderRadius: XActRadius.md),
          elevation: 0,
        ),
      ),
    );
  }

  static Widget _filled(
    String text,
    VoidCallback? onPressed,
    Color bg,
    double height,
    TextStyle textStyle,
  ) {
    return SizedBox(
      width: double.infinity,
      height: height,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: bg,
          foregroundColor: XActColors.text1,
          elevation: 0,
          shape: const RoundedRectangleBorder(borderRadius: XActRadius.md),
        ),
        child: Text(text, style: textStyle),
      ),
    );
  }

  // ─── Cards / forms ────────────────────────────────────────────────
  static Widget buildFormCard({required Widget child}) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(XActSpace.s5),
      decoration: const BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
      ),
      child: child,
    );
  }

  /// Reusable horizontal action card — replaces the duplicated
  /// `InkWell`/`Container` blocks in `playnow_screen.dart`.
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
        child: Padding(
          padding: const EdgeInsets.all(XActSpace.s5),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, color: XActColors.text1, size: 28),
              const SizedBox(width: XActSpace.s4),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: XActText.subheading.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: XActSpace.s2),
                    Text(
                      subtitle,
                      style: XActText.bodySm.copyWith(color: XActColors.text2),
                    ),
                  ],
                ),
              ),
            ],
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
          style: XActText.bodySm.copyWith(color: XActColors.text2),
        ),
        const SizedBox(height: XActSpace.s2),
        TextField(
          controller: controller,
          keyboardType: keyboardType,
          maxLength: maxLength,
          inputFormatters: inputFormatters,
          textCapitalization: textCapitalization,
          style: XActText.subheading,
          decoration: InputDecoration(
            hintText: hintText,
            hintStyle: XActText.subheading.copyWith(color: XActColors.text4),
            filled: true,
            fillColor: XActColors.bg,
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
              borderSide: BorderSide(color: XActColors.secondary),
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: XActSpace.s4,
              vertical: XActSpace.s4,
            ),
          ),
        ),
      ],
    );
  }
}
