import 'package:flutter/material.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/screens/auth/login_screen.dart';
import 'package:xact_frontend/screens/game_screen.dart';
import 'package:xact_frontend/screens/settings/profile_screen.dart';
import 'package:xact_frontend/screens/start/playnow_screen.dart';
import 'package:xact_frontend/services/active_game_storage.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/user_avatar.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class StartScreen extends StatefulWidget {
  const StartScreen({super.key});

  @override
  State<StartScreen> createState() => _StartScreenState();
}

class _StartScreenState extends State<StartScreen> {
  bool _showHowTo = false;
  ({ActiveGame game, String sessionName})? _resumable;
  bool _resuming = false;
  MyProfile? _profile;

  // guests get a user id too, so only a keycloak token means signed in
  bool get _isLoggedIn => ApiService.instance.isAuthenticated;

  @override
  void initState() {
    super.initState();
    _loadResumableGame();
    _loadProfile();
  }

  Future<void> _loadResumableGame() async {
    final resumable = await ApiService.instance.loadResumableGame();
    if (mounted) setState(() => _resumable = resumable);
  }

  Future<void> _rejoinGame() async {
    final resumable = _resumable;
    if (resumable == null || _resuming) return;

    setState(() => _resuming = true);
    await ApiService.instance.resumeGame(resumable.game);
    if (!mounted) return;

    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => const GameScreen()),
      (_) => false,
    );
  }

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
                      if (_resumable != null) ...[
                        XActBranding.buildSuccessButton(
                          text: _resuming
                              ? 'Rejoining…'
                              : 'Rejoin ${_resumable!.sessionName}',
                          icon: Icons.replay_rounded,
                          onPressed: _resuming ? null : _rejoinGame,
                        ),
                        const SizedBox(height: XActSpace.s3),
                      ],
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

  Future<void> _openSettings() async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => const ProfileScreen()),
    );
    setState(() {});
    _loadProfile();
  }

  /// only for the avatar in the corner, which falls back to the initials
  /// until this loads or when it fails
  Future<void> _loadProfile() async {
    if (!_isLoggedIn) return;

    try {
      final profile = await ApiService.instance.loadMyProfile();
      if (mounted) setState(() => _profile = profile);
    } catch (_) {}
  }

  Widget _buildTopBar() {
    final username = AppSession.instance.currentUsername;
    if (!_isLoggedIn || username == null) {
      return Align(
        alignment: Alignment.topRight,
        child: Padding(
          padding: const EdgeInsets.only(top: 12, right: 16),
          child: XActBranding.circleIconButton(
            icon: Icons.settings_outlined,
            onPressed: _openSettings,
          ),
        ),
      );
    }

    final profile = _profile;

    return Align(
      alignment: Alignment.topRight,
      child: Padding(
        padding: const EdgeInsets.only(top: 12, right: 16),
        child: GestureDetector(
          onTap: _openSettings,
          child: UserAvatar(
            name: username,
            emoji: profile?.avatarEmoji,
            color: profile?.avatarColor,
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
