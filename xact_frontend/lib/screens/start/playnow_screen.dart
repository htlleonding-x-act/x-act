import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/lobby/create_lobby.dart';
import 'package:xact_frontend/screens/lobby/join_lobby.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class PlayNowScreen extends StatelessWidget {
  const PlayNowScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: SafeArea(
        child: Column(
          children: [
            XActBranding.buildTopBar(
              context: context,
              eyebrow: 'Get started',
              title: 'Play',
            ),
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _CreateGameHero(
                      onTap: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => const CreateGameScreen(),
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),
                    XActBranding.buildActionCard(
                      icon: Icons.arrow_forward_rounded,
                      title: "Join a Friend's Game",
                      subtitle: 'Enter a code or scan a QR',
                      onTap: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => const JoinGameScreen(),
                        ),
                      ),
                    ),
                    const SizedBox(height: XActSpace.s6),
                    const _HowItWorks(),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CreateGameHero extends StatelessWidget {
  final VoidCallback onTap;
  const _CreateGameHero({required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      borderRadius: BorderRadius.circular(24),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(24),
        child: Ink(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(24),
            gradient: const LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [XActColors.secondaryDark, Color(0xFF2E3FA0)],
            ),
            boxShadow: XActElevation.glowBlue,
            border: Border.all(
              color: Colors.white.withValues(alpha: .18),
            ),
          ),
          child: Stack(
            clipBehavior: Clip.hardEdge,
            children: [
              Positioned(
                top: -40,
                right: -40,
                child: Container(
                  width: 200,
                  height: 200,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    gradient: RadialGradient(
                      colors: [
                        Colors.white.withValues(alpha: .18),
                        Colors.white.withValues(alpha: 0),
                      ],
                    ),
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(22),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: .18),
                        borderRadius: BorderRadius.circular(14),
                      ),
                      child: const Icon(
                        Icons.add_rounded,
                        color: Colors.white,
                        size: 26,
                      ),
                    ),
                    const SizedBox(height: 14),
                    Text(
                      'Start a New Game',
                      style: XActText.title.copyWith(fontSize: 22),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      'Create a lobby, define the play area, invite friends with a code or QR.',
                      style: XActText.body.copyWith(
                        fontSize: 14,
                        color: Colors.white.withValues(alpha: .78),
                        height: 1.45,
                      ),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      children: [
                        Text(
                          'Create lobby',
                          style: XActText.bodySm.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(width: 6),
                        const Icon(
                          Icons.arrow_forward_rounded,
                          color: Colors.white,
                          size: 16,
                        ),
                      ],
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
}

class _HowItWorks extends StatelessWidget {
  const _HowItWorks();

  @override
  Widget build(BuildContext context) {
    const steps = [
      (1, 'Create or join', 'A game code links your crew'),
      (2, 'Pick a side', 'Become Mister X — or hunt them'),
      (3, 'Hit the streets', 'Real city. Real chase.'),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 12),
          child: XActBranding.buildEyebrow('How it works'),
        ),
        for (final step in steps) ...[
          Container(
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
                      '${step.$1}',
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
                        step.$2,
                        style: XActText.bodySm.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        step.$3,
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
          ),
          if (step.$1 != 3) const SizedBox(height: 10),
        ],
      ],
    );
  }
}
