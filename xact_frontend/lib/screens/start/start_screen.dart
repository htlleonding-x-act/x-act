import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/start/playnow_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class StartScreen extends StatefulWidget {
  const StartScreen({super.key});

  @override
  State<StartScreen> createState() => _StartScreenState();
}

class _StartScreenState extends State<StartScreen> {
  bool _isExpanded = false;

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
              _buildHowToGetStartedDropdown(),
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

  Widget _buildHowToGetStartedDropdown() {
    return Column(
      children: [
        SizedBox(
          width: double.infinity,
          height: 56,
          child: OutlinedButton.icon(
            onPressed: () {
              setState(() {
                _isExpanded = !_isExpanded;
              });
            },
            style: OutlinedButton.styleFrom(
              side: const BorderSide(color: Colors.white24),
              backgroundColor: XActBranding.cardColor,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
            icon: const Icon(Icons.help_outline, color: Colors.white70),
            label: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text(
                  'How to Get Started',
                  style: TextStyle(color: Colors.white, fontSize: 16),
                ),
                const SizedBox(width: 8),
                AnimatedRotation(
                  turns: _isExpanded ? 0.5 : 0,
                  duration: const Duration(milliseconds: 200),
                  child: const Icon(
                    Icons.expand_more,
                    color: Colors.white70,
                    size: 20,
                  ),
                ),
              ],
            ),
          ),
        ),
        AnimatedCrossFade(
          firstChild: const SizedBox.shrink(),
          secondChild: Padding(
            padding: const EdgeInsets.only(top: 12),
            child: _buildStepsCard(),
          ),
          crossFadeState: _isExpanded
              ? CrossFadeState.showSecond
              : CrossFadeState.showFirst,
          duration: const Duration(milliseconds: 200),
        ),
      ],
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
