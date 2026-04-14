import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class XActBranding {
  XActBranding._();

  static const Color primaryRed = Color(0xFFE53935);
  static const Color backgroundColor = Color(0xFF1A1F2E);
  static const Color cardColor = Color(0xFF252A3A);
  static const Color primaryBlue = Color(0xFF3D5AFE);

  static Widget buildLogo({double size = 80}) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: primaryRed, width: 3),
      ),
      child: Center(
        child: Icon(Icons.location_on, color: primaryRed, size: size * 0.5),
      ),
    );
  }

  static Widget buildHeader({bool compact = false}) {
    return Column(
      children: [
        buildLogo(),
        const SizedBox(height: 16),
        const Text(
          'X-ACT',
          style: TextStyle(
            fontSize: 48,
            fontWeight: FontWeight.bold,
            color: Colors.white,
            letterSpacing: 2,
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          'Digital Scotland Yard',
          style: TextStyle(
            fontSize: 18,
            color: Colors.white70,
            letterSpacing: 1,
          ),
        ),
        const SizedBox(height: 12),
        const Text(
          'Hunt down Mister X in this real-world chase game',
          style: TextStyle(fontSize: 14, color: Colors.white54),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }

  static Widget buildFooter() {
    return const Text(
      'Play responsibly • Stay safe • Follow local laws',
      style: TextStyle(fontSize: 12, color: Colors.white38),
    );
  }

  static Widget buildPrimaryButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 64,
  }) {
    return SizedBox(
      width: double.infinity,
      height: height,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryRed,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          elevation: 0,
        ),
        child: Text(
          text,
          style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
      ),
    );
  }

  static Widget buildSecondaryButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 56,
  }) {
    return SizedBox(
      width: double.infinity,
      height: height,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          elevation: 0,
        ),
        child: Text(
          text,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }

  static Widget buildCancelButton({
    required String text,
    required VoidCallback? onPressed,
    double height = 56,
  }) {
    return SizedBox(
      width: double.infinity,
      height: height,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: cardColor,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          elevation: 0,
        ),
        child: Text(
          text,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }

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
          style: const TextStyle(color: Colors.white70, fontSize: 14),
        ),
        const SizedBox(height: 8),
        TextField(
          controller: controller,
          keyboardType: keyboardType,
          maxLength: maxLength,
          inputFormatters: inputFormatters,
          textCapitalization: textCapitalization,
          style: const TextStyle(color: Colors.white, fontSize: 18),
          decoration: InputDecoration(
            hintText: hintText,
            hintStyle: const TextStyle(color: Colors.white38, fontSize: 18),
            filled: true,
            fillColor: backgroundColor,
            counterText: '',
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: Colors.white24),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: Colors.white24),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: primaryBlue),
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 16,
              vertical: 16,
            ),
          ),
        ),
      ],
    );
  }

  static Widget buildFormCard({required Widget child}) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
      ),
      child: child,
    );
  }
}
