import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final session = AppSession.instance;
    final username = session.currentUsername ?? 'Player';
    final initials = _initials(username);

    return Scaffold(
      backgroundColor: XActColors.bg,
      body: Stack(
        children: [
          Positioned.fill(child: XActBranding.aurora()),
          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                XActBranding.buildTopBar(
                  context: context,
                  eyebrow: 'Account',
                  title: 'Profile',
                ),
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _buildAvatar(initials, username),
                        const SizedBox(height: 28),
                        _buildSection(
                          label: 'Account',
                          children: [
                            _buildInfoRow(
                              icon: Icons.person_outline_rounded,
                              title: 'Username',
                              value: username,
                            ),
                            _buildDivider(),
                            _buildInfoRow(
                              icon: Icons.badge_outlined,
                              title: 'Account type',
                              value: 'Free',
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        _buildSection(
                          label: 'Preferences',
                          children: [
                            _buildSettingsTile(
                              icon: Icons.notifications_outlined,
                              title: 'Notifications',
                              subtitle: 'Coming soon',
                            ),
                            _buildDivider(),
                            _buildSettingsTile(
                              icon: Icons.palette_outlined,
                              title: 'Appearance',
                              subtitle: 'Coming soon',
                            ),
                            _buildDivider(),
                            _buildSettingsTile(
                              icon: Icons.lock_outline_rounded,
                              title: 'Privacy',
                              subtitle: 'Coming soon',
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        _buildSection(
                          label: 'About',
                          children: [
                            _buildInfoRow(
                              icon: Icons.info_outline_rounded,
                              title: 'Version',
                              value: '1.0.0-dev',
                            ),
                          ],
                        ),
                        const SizedBox(height: 28),
                        _buildLogoutButton(context),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAvatar(String initials, String username) {
    return Center(
      child: Column(
        children: [
          Container(
            width: 88,
            height: 88,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [XActColors.primaryLight, XActColors.primaryDark],
              ),
              boxShadow: XActElevation.glowRed,
              border: Border.all(
                color: Colors.white.withValues(alpha: .12),
                width: 2,
              ),
            ),
            child: Center(
              child: Text(
                initials,
                style: GoogleFonts.spaceGrotesk(
                  fontSize: 30,
                  fontWeight: FontWeight.w700,
                  color: Colors.white,
                  letterSpacing: -1,
                ),
              ),
            ),
          ),
          const SizedBox(height: 14),
          Text(username, style: XActText.title),
          const SizedBox(height: 4),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              color: XActColors.secondarySoft,
              borderRadius: XActRadius.pill,
            ),
            child: Text(
              'PLAYER',
              style: GoogleFonts.inter(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: XActColors.secondary,
                letterSpacing: 1.2,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSection({
    required String label,
    required List<Widget> children,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 8),
          child: XActBranding.buildEyebrow(label),
        ),
        Container(
          decoration: BoxDecoration(
            color: XActColors.surface,
            borderRadius: XActRadius.lg,
            border: Border.all(color: XActColors.hairlineSoft),
            boxShadow: XActElevation.e1,
          ),
          child: Column(children: children),
        ),
      ],
    );
  }

  Widget _buildInfoRow({
    required IconData icon,
    required String title,
    required String value,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(
        children: [
          Icon(icon, size: 20, color: XActColors.text3),
          const SizedBox(width: 14),
          Expanded(child: Text(title, style: XActText.bodySm)),
          Text(
            value,
            style: XActText.bodySm.copyWith(color: XActColors.text3),
          ),
        ],
      ),
    );
  }

  Widget _buildSettingsTile({
    required IconData icon,
    required String title,
    required String subtitle,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(
        children: [
          Icon(icon, size: 20, color: XActColors.text4),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: XActText.bodySm.copyWith(color: XActColors.text4),
                ),
                Text(subtitle, style: XActText.caption.copyWith(fontSize: 12)),
              ],
            ),
          ),
          Icon(Icons.chevron_right_rounded, size: 18, color: XActColors.text5),
        ],
      ),
    );
  }

  Widget _buildDivider() {
    return Divider(
      height: 1,
      thickness: 1,
      color: XActColors.hairlineFaint,
      indent: 50,
    );
  }

  Widget _buildLogoutButton(BuildContext context) {
    return Material(
      color: XActColors.surface,
      borderRadius: XActRadius.lg,
      child: InkWell(
        onTap: () => _confirmLogout(context),
        borderRadius: XActRadius.lg,
        child: Ink(
          decoration: BoxDecoration(
            borderRadius: XActRadius.lg,
            border: Border.all(
              color: XActColors.primary.withValues(alpha: .25),
            ),
          ),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(
                  Icons.logout_rounded,
                  size: 20,
                  color: XActColors.primary,
                ),
                const SizedBox(width: 10),
                Text(
                  'Log out',
                  style: XActText.bodySm.copyWith(
                    color: XActColors.primary,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Log out?', style: XActText.heading),
        content: Text(
          'You\'ll need to sign in again to play.',
          style: XActText.body.copyWith(color: XActColors.text3, fontSize: 14),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: Text(
              'Cancel',
              style: XActText.bodySm.copyWith(color: XActColors.text3),
            ),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: Text(
              'Log out',
              style: XActText.bodySm.copyWith(
                color: XActColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      await ApiService.instance.logout();
      if (context.mounted) {
        Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(builder: (_) => const StartScreen()),
          (_) => false,
        );
      }
    }
  }

  static String _initials(String name) {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
    }
    return name.substring(0, name.length.clamp(1, 2)).toUpperCase();
  }
}
