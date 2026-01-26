import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/start/playnow_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class GetStartedScreen extends StatelessWidget {
  const GetStartedScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActBranding.backgroundColor,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: Column(
            children: [
              const SizedBox(height: 24),
              XActBranding.buildHeader(),
              const SizedBox(height: 24),
              // Hide Help Button
              _buildHideHelpButton(context),
              const SizedBox(height: 16),
              // Steps Card
              _buildStepsCard(),
              const Spacer(),
              // Play Now Button
              _buildPlayNowButton(context),
              const SizedBox(height: 24),
              // Footer text
              XActBranding.buildFooter(),
              const SizedBox(height: 16),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHideHelpButton(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 56,
      child: OutlinedButton.icon(
        onPressed: () {
          Navigator.of(context).pop();
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
          'Hide Help',
          style: TextStyle(color: Colors.white, fontSize: 16),
        ),
      ),
    );
  }

  Widget _buildStepsCard() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: XActBranding.cardColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          _buildStepItem(1, 'Create a new game or join a friends game'),
          const SizedBox(height: 16),
          _buildStepItem(2, 'Choose your team (Mister X or Detectives)'),
          const SizedBox(height: 16),
          _buildStepItem(3, 'Wait for all players to pick their team'),
          const SizedBox(height: 16),
          _buildStepItem(4, 'Host starts the game and the fun begins!'),
        ],
      ),
    );
  }

  Widget _buildStepItem(int number, String text) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 28,
          height: 28,
          decoration: const BoxDecoration(
            color: XActBranding.primaryRed,
            shape: BoxShape.circle,
          ),
          child: Center(
            child: Text(
              number.toString(),
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 14,
              ),
            ),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Text(
              text,
              style: const TextStyle(color: Colors.white, fontSize: 15),
            ),
          ),
        ),
      ],
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
