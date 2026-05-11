import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/start/playnow_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class StartScreen extends StatefulWidget {
  const StartScreen({super.key});

  @override
  State<StartScreen> createState() => _StartScreenState();
}

class _StartScreenState extends State<StartScreen> {
  bool _showHowTo = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: Stack(
        children: [
          Positioned.fill(child: XActBranding.aurora()),
          SafeArea(
            child: Column(
              children: [
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 28),
                    child: Center(
                      child: SingleChildScrollView(
                        child: XActBranding.buildHeader(),
                      ),
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(24, 0, 24, 28),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      AnimatedSize(
                        duration: const Duration(milliseconds: 220),
                        curve: Curves.easeOutCubic,
                        alignment: Alignment.bottomCenter,
                        child: _showHowTo
                            ? Padding(
                                padding: const EdgeInsets.only(
                                  bottom: XActSpace.s3,
                                ),
                                child: _buildHowToCard(),
                              )
                            : const SizedBox(width: double.infinity),
                      ),
                      XActBranding.buildGhostButton(
                        text: 'How to play',
                        icon: Icons.help_outline_rounded,
                        trailing: Icon(
                          _showHowTo
                              ? Icons.expand_more_rounded
                              : Icons.expand_less_rounded,
                          color: XActColors.text3,
                          size: 18,
                        ),
                        onPressed: () =>
                            setState(() => _showHowTo = !_showHowTo),
                      ),
                      const SizedBox(height: XActSpace.s3),
                      XActBranding.buildPrimaryButton(
                        text: 'Play Now',
                        icon: Icons.play_arrow_rounded,
                        onPressed: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => const PlayNowScreen(),
                          ),
                        ),
                      ),
                      const SizedBox(height: XActSpace.s4),
                      Center(child: XActBranding.buildFooter()),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHowToCard() {
    final steps = [
      ('Create or join', 'A game code links your crew'),
      ('Pick a side', 'Become Mister X — or hunt them'),
      ('Hit the streets', 'Real city. Real chase.'),
    ];

    return Column(
      children: [
        for (int i = 0; i < steps.length; i++) ...[
          if (i > 0) const SizedBox(height: XActSpace.s2),
          _buildStep(i + 1, steps[i].$1, steps[i].$2),
        ],
      ],
    );
  }

  Widget _buildStep(int n, String title, String subtitle) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e1,
      ),
      child: Row(
        children: [
          Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: XActColors.primarySoft,
            ),
            child: Center(
              child: Text(
                '$n',
                style: XActText.bodySm.copyWith(
                  color: XActColors.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: XActText.bodySm.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  subtitle,
                  style: XActText.caption.copyWith(
                    fontSize: 12,
                    color: XActColors.text3,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
