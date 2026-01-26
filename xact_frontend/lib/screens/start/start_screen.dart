import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/start/get_started.dart';
import 'package:xact_frontend/screens/start/playnow_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class StartScreen extends StatelessWidget {
  const StartScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActBranding.backgroundColor,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: Column(
            children: [
              const Spacer(flex: 2),
              XActBranding.buildHeader(),
              const Spacer(flex: 2),
              _buildHowToGetStartedButton(context),
              const SizedBox(height: 16),
              _buildPlayNowButton(context),
              const SizedBox(height: 24),
              XActBranding.buildFooter(),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHowToGetStartedButton(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 56,
      child: OutlinedButton.icon(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => const GetStartedScreen()),
          );
        },
        style: OutlinedButton.styleFrom(
          side: const BorderSide(color: Colors.white24),
          backgroundColor: XActBranding.cardColor,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        icon: const Icon(Icons.help_outline, color: Colors.white70),
        label: const Text(
          'How to Get Started',
          style: TextStyle(color: Colors.white, fontSize: 16),
        ),
      ),
    );
  }

  Widget _buildPlayNowButton(BuildContext context) {
    return XActBranding.buildPrimaryButton(
      text: 'Play Now',
      onPressed: () {
        Navigator.push(
          context,
          MaterialPageRoute(builder: (context) => const PlayNowScreen()),
        );
      },
    );
  }
}
