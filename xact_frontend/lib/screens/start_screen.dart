import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/get_started.dart';
import 'package:xact_frontend/screens/playnow_screen.dart';

class StartScreen extends StatelessWidget {
  const StartScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1A1F2E),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: Column(
            children: [
              const Spacer(flex: 2),
              _buildLogo(),
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
              const Spacer(flex: 2),
              _buildHowToGetStartedButton(context),
              const SizedBox(height: 16),
              _buildPlayNowButton(context),
              const SizedBox(height: 24),
              const Text(
                'Play responsibly • Stay safe • Follow local laws',
                style: TextStyle(fontSize: 12, color: Colors.white38),
              ),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLogo() {
    return Container(
      width: 80,
      height: 80,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: const Color(0xFFE53935), width: 3),
      ),
      child: const Center(
        child: Icon(Icons.location_on, color: Color(0xFFE53935), size: 40),
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
          backgroundColor: const Color(0xFF252A3A),
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
    return SizedBox(
      width: double.infinity,
      height: 64,
      child: ElevatedButton(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (context) => const PlayNowScreen()),
          );
        },
        style: ElevatedButton.styleFrom(
          backgroundColor: const Color(0xFFE53935),
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          elevation: 0,
        ),
        child: const Text(
          'Play Now',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
      ),
    );
  }
}
