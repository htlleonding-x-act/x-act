import 'package:flutter/material.dart';
import 'package:xact_frontend/screens/auth/login_screen.dart';
import 'package:xact_frontend/screens/settings/profile_screen.dart';
import 'package:xact_frontend/screens/start/playnow_screen.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class StartScreen extends StatefulWidget {
  const StartScreen({super.key});

  @override
  State<StartScreen> createState() => _StartScreenState();
}

class _StartScreenState extends State<StartScreen> {
  bool _showHowTo = false;

  bool get _isLoggedIn => AppSession.instance.currentUserId != null;

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
                _buildTopBar(),
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
                      if (_isLoggedIn) ...[
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
                      ] else ...[
                        XActBranding.buildPrimaryButton(
                          text: 'Login · Register',
                          icon: Icons.login_rounded,
                          onPressed: () => Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const LoginScreen(),
                            ),
                          ),
                        ),
                        const SizedBox(height: XActSpace.s3),
                        XActBranding.buildGhostButton(
                          text: 'Continue as Guest',
                          icon: Icons.person_outline_rounded,
                          onPressed: () => Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const PlayNowScreen(),
                            ),
                          ),
                        ),
                      ],
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

  Widget _buildTopBar() {
    final username = AppSession.instance.currentUsername;
    if (username == null) return const SizedBox.shrink();

    final initial = username.trim().isEmpty
        ? 'P'
        : username.trim().substring(0, 1).toUpperCase();

    return Align(
      alignment: Alignment.topRight,
      child: Padding(
        padding: const EdgeInsets.only(top: 12, right: 16),
        child: GestureDetector(
          onTap: () async {
            await Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const ProfileScreen()),
            );
            setState(() {});
          },
          child: Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [XActColors.primaryLight, XActColors.primaryDark],
              ),
              border: Border.all(color: Colors.white.withValues(alpha: .15)),
              boxShadow: XActElevation.e1,
            ),
            child: Center(
              child: Text(
                initial,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
        ),
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
                  style: XActText.bodySm.copyWith(fontWeight: FontWeight.w600),
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
