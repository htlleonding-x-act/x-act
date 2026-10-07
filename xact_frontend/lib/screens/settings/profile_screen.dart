import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/api/models.dart';
import 'package:xact_frontend/auth/auth_config.dart';
import 'package:xact_frontend/screens/auth/login_screen.dart';
import 'package:xact_frontend/screens/settings/account_dialogs.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/services/app_session.dart';
import 'package:xact_frontend/widgets/settings/settings_widgets.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  static const _months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];

  MyProfile? _profile;
  bool _profileFailed = false;
  String? _version;

  // guests get a user id too, so only a keycloak token means signed in
  bool get _isSignedIn => ApiService.instance.isAuthenticated;

  String get _username =>
      _profile?.username ?? AppSession.instance.currentUsername ?? 'Player';

  @override
  void initState() {
    super.initState();
    _loadVersion();
    if (_isSignedIn) _loadProfile();
  }

  Future<void> _loadProfile() async {
    setState(() => _profileFailed = false);
    try {
      final profile = await ApiService.instance.loadMyProfile();
      if (mounted) setState(() => _profile = profile);
    } catch (_) {
      if (mounted) setState(() => _profileFailed = true);
    }
  }

  Future<void> _loadVersion() async {
    try {
      final info = await PackageInfo.fromPlatform();
      final build = info.buildNumber.isEmpty ? '' : ' (${info.buildNumber})';
      if (mounted) setState(() => _version = '${info.version}$build');
    } catch (_) {}
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
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                XActBranding.buildTopBar(
                  context: context,
                  eyebrow: 'Account',
                  title: 'Settings',
                ),
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.fromLTRB(20, 0, 20, 32),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _buildHeader(),
                        const SizedBox(height: 28),
                        if (_isSignedIn)
                          _buildAccountSection()
                        else
                          _buildGuestCard(),
                        const SizedBox(height: 16),
                        _buildAboutSection(),
                        const SizedBox(height: 28),
                        _buildLogoutButton(),
                        if (_isSignedIn) ...[
                          const SizedBox(height: 28),
                          _buildDangerZone(),
                        ],
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

  Widget _buildHeader() {
    final createdAt = _profile?.createdAt;

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
                _initials(_username),
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
          Text(_username, style: XActText.title, textAlign: TextAlign.center),
          const SizedBox(height: 6),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            decoration: BoxDecoration(
              color: XActColors.secondarySoft,
              borderRadius: XActRadius.pill,
            ),
            child: Text(
              _accountLabel.toUpperCase(),
              style: GoogleFonts.inter(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: XActColors.secondary,
                letterSpacing: 1.2,
              ),
            ),
          ),
          if (createdAt != null) ...[
            const SizedBox(height: 8),
            Text(
              'Member since ${_months[createdAt.month - 1]} ${createdAt.year}',
              style: XActText.caption,
            ),
          ],
        ],
      ),
    );
  }

  String get _accountLabel {
    if (!_isSignedIn) return 'Guest';

    return switch (_profile?.accountType) {
      AccountType.pro => 'Pro',
      AccountType.eventPass => 'Event Pass',
      AccountType.free || null => 'Free',
    };
  }

  Widget _buildAccountSection() {
    final email = _profile?.email;

    return SettingsSection(
      label: 'Account',
      children: [
        SettingsTile(
          icon: Icons.person_outline_rounded,
          title: 'Username',
          subtitle: _username,
          trailingIcon: Icons.edit_outlined,
          onTap: _rename,
        ),
        const SettingsDivider(),
        SettingsInfoRow(
          icon: Icons.mail_outline_rounded,
          title: 'Email',
          value: email ?? (_profile == null ? '…' : 'Not set'),
        ),
        const SettingsDivider(),
        SettingsTile(
          icon: Icons.shield_outlined,
          title: 'Password, email & 2FA',
          subtitle: 'Opens your X-ACT login account',
          trailingIcon: Icons.open_in_new_rounded,
          onTap: _openAccountConsole,
        ),
        if (_profileFailed) ...[
          const SettingsDivider(),
          SettingsTile(
            icon: Icons.cloud_off_rounded,
            title: 'Could not load your profile',
            subtitle: 'Tap to try again',
            trailingIcon: Icons.refresh_rounded,
            onTap: _loadProfile,
          ),
        ],
      ],
    );
  }

  Widget _buildGuestCard() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: XActColors.surface,
        borderRadius: XActRadius.lg,
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e1,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('Playing as a guest', style: XActText.heading),
          const SizedBox(height: 6),
          Text(
            'Create an account to keep your name, pick an avatar and see '
            'your stats across matches.',
            style: XActText.body.copyWith(
              color: XActColors.text3,
              fontSize: 14,
            ),
          ),
          const SizedBox(height: 16),
          XActBranding.buildPrimaryButton(
            text: 'Login · Register',
            icon: Icons.login_rounded,
            height: 52,
            onPressed: () => Navigator.of(
              context,
            ).push(MaterialPageRoute(builder: (_) => const LoginScreen())),
          ),
        ],
      ),
    );
  }

  Widget _buildAboutSection() {
    return SettingsSection(
      label: 'About',
      children: [
        SettingsInfoRow(
          icon: Icons.info_outline_rounded,
          title: 'Version',
          value: _version ?? '…',
        ),
        const SettingsDivider(),
        SettingsTile(
          icon: Icons.description_outlined,
          title: 'Open source licences',
          onTap: () => showLicensePage(
            context: context,
            applicationName: 'X-ACT',
            applicationVersion: _version,
          ),
        ),
      ],
    );
  }

  Widget _buildDangerZone() {
    return SettingsSection(
      label: 'Danger zone',
      borderColor: XActColors.primary.withValues(alpha: .25),
      children: [
        SettingsTile(
          icon: Icons.delete_forever_outlined,
          title: 'Delete account',
          subtitle: 'Removes your name and email from X-ACT',
          color: XActColors.primary,
          onTap: _deleteAccount,
        ),
      ],
    );
  }

  Widget _buildLogoutButton() {
    return Material(
      color: XActColors.surface,
      borderRadius: XActRadius.lg,
      child: InkWell(
        onTap: _confirmLogout,
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

  Future<void> _rename() async {
    final renamed = await showRenameDialog(context, _username);
    if (renamed) _loadProfile();
  }

  Future<void> _openAccountConsole() async {
    final uri = Uri.parse('${AuthConfig.authority}/account');
    final opened = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!opened && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not open your login account.')),
      );
    }
  }

  Future<void> _deleteAccount() async {
    final deleted = await showDeleteAccountDialog(context, _username);
    if (deleted) _returnToStart();
  }

  Future<void> _confirmLogout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Log out?', style: XActText.heading),
        content: Text(
          // a guest can't sign back in, the guest identity is gone for good
          _isSignedIn
              ? 'You\'ll need to sign in again to play.'
              : 'You\'ll leave the current game and lose your guest name.',
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

    if (confirmed == true && mounted) {
      await ApiService.instance.logout();
      _returnToStart();
    }
  }

  void _returnToStart() {
    if (!mounted) return;

    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => const StartScreen()),
      (_) => false,
    );
  }

  static String _initials(String name) {
    // characters, not code units, so a name starting with an emoji keeps it
    // whole instead of showing half of it
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return '${parts.first.characters.first}${parts.last.characters.first}'
          .toUpperCase();
    }
    return name.characters.take(2).toString().toUpperCase();
  }
}
