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
      backgroundColor: XActColors.bg,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: XActSpace.s6),
          child: Column(
            children: [
              const Spacer(flex: 2),
              XActBranding.buildHeader(),
              const Spacer(flex: 2),
              _buildHowToGetStartedDropdown(),
              const SizedBox(height: XActSpace.s4),
              _buildPlayNowButton(context),
              const SizedBox(height: XActSpace.s6),
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
              side: BorderSide(color: XActColors.hairlineSoft),
              backgroundColor: XActColors.surface,
              shape: const RoundedRectangleBorder(
                borderRadius: XActRadius.md,
              ),
            ),
            icon: Icon(Icons.help_outline, color: XActColors.text2),
            label: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text('How to Get Started', style: XActText.body),
                const SizedBox(width: XActSpace.s2),
                AnimatedRotation(
                  turns: _isExpanded ? 0.5 : 0,
                  duration: const Duration(milliseconds: 200),
                  child: Icon(
                    Icons.expand_more,
                    color: XActColors.text2,
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
            padding: const EdgeInsets.only(top: XActSpace.s3),
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
      padding: const EdgeInsets.all(XActSpace.s5),
      decoration: const BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.md,
      ),
      child: Column(
        children: [
          _buildStepItem(1, 'Create a new game or join a friends game'),
          const SizedBox(height: XActSpace.s4),
          _buildStepItem(2, 'Choose your team (Mister X or Detectives)'),
          const SizedBox(height: XActSpace.s4),
          _buildStepItem(3, 'Wait for all players to pick their team'),
          const SizedBox(height: XActSpace.s4),
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
            color: XActColors.primary,
            shape: BoxShape.circle,
          ),
          child: Center(
            child: Text(
              number.toString(),
              style: XActText.bodySm.copyWith(fontWeight: FontWeight.bold),
            ),
          ),
        ),
        const SizedBox(width: XActSpace.s3),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(top: XActSpace.s1),
            child: Text(text, style: XActText.body.copyWith(fontSize: 15)),
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
