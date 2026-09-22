import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:app_links/app_links.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/auth/auth_challenge.dart';
import 'package:xact_frontend/auth/auth_config.dart';
import 'package:xact_frontend/auth/auth_storage.dart';
import 'package:xact_frontend/auth/local_callback_server.dart';
import 'package:xact_frontend/auth/web_url_cleaner.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  bool _isLoading = false;
  StreamSubscription? _deepLinkSub;
  Object? _callbackServer;

  @override
  void initState() {
    super.initState();

    // On web the callback is a fresh load of this app with ?code=... in the URL, so
    // finishing that login must not start another one.
    if (kIsWeb && Uri.base.queryParameters.containsKey('code')) {
      _handleCallback(Uri.base);
      return;
    }

    _initCallbackListener();
    WidgetsBinding.instance.addPostFrameCallback((_) => _launchKeycloak());
  }

  // ── Callback listener setup ───────────────────────────────────────────────

  Future<void> _initCallbackListener() async {
    if (kIsWeb) {
      return;
    }

    if (AuthConfig.isDesktop) {
      // Desktop: spin up a local HTTP server so no custom URI scheme is needed.
      _callbackServer = await startLocalCallbackServer(
        AuthConfig.desktopCallbackPort,
        _handleCallback,
      );
      return;
    }

    // Mobile (Android / iOS): the stream also replays the link that cold-started the app.
    _deepLinkSub = AppLinks().uriLinkStream.listen(_handleCallback, onError: (_) {});
  }

  // ── Keycloak browser launch ───────────────────────────────────────────────

  Future<void> _launchKeycloak() async {
    if (_isLoading) return;
    _setLoading(true);

    final challenge = AuthChallenge.generate();
    await AuthStorage.savePendingChallenge(challenge);

    // On web, open in a new tab — Keycloak will redirect back to this origin.
    // On desktop/mobile, open in an external application.
    final launched = await launchUrl(
      AuthConfig.loginUri(challenge),
      mode: kIsWeb ? LaunchMode.platformDefault : LaunchMode.externalApplication,
    );

    if (!launched) {
      _showError('Could not open the login page. Please try again.');
    }

    _setLoading(false);
  }

  // ── Code-exchange handler ─────────────────────────────────────────────────

  Future<void> _handleCallback(Uri uri) async {
    final code = uri.queryParameters['code'];
    if (code == null) return;

    _setLoading(true);

    final pending = await AuthStorage.loadPendingChallenge();
    await AuthStorage.clearPendingChallenge();

    // A callback carrying someone else's state was not started by this app, and the
    // code is worthless without the verifier that belongs to it.
    if (pending == null || pending.state != uri.queryParameters['state']) {
      cleanBrowserUrl();
      _setLoading(false);
      _showError('Login failed. Please try again.');
      return;
    }

    final success = await ApiService.instance.exchangeAuthCode(
      code,
      pending.verifier,
    );

    _setLoading(false);

    if (!mounted) return;

    if (success) {
      cleanBrowserUrl();
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const StartScreen()),
      );
    } else {
      _showError('Login failed. Please try again.');
    }
  }

  void _showError(String message) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(message)),
      );
    }
  }

  void _setLoading(bool value) {
    if (mounted) setState(() => _isLoading = value);
  }

  // ── Build ─────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: XActColors.bg,
      body: Stack(
        children: [
          Positioned.fill(child: XActBranding.aurora()),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 420),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      XActBranding.buildHeader(),
                      const SizedBox(height: 28),
                      _buildCard(),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCard() {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: XActColors.surface.withValues(alpha: .96),
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: XActColors.hairlineSoft),
        boxShadow: XActElevation.e2,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Sign in to play',
            style: XActText.title.copyWith(fontSize: 24),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 10),
          Text(
            _isLoading
                ? 'Waiting for authentication...'
                : 'You will be redirected to log in.',
            style: XActText.body.copyWith(color: XActColors.text2, height: 1.45),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 22),
          if (_isLoading)
            const Center(child: CircularProgressIndicator())
          else
            XActBranding.buildPrimaryButton(
              text: 'Login',
              icon: Icons.login_rounded,
              onPressed: _launchKeycloak,
            ),
        ],
      ),
    );
  }

  // ── Cleanup ───────────────────────────────────────────────────────────────

  @override
  void dispose() {
    _deepLinkSub?.cancel();
    stopLocalCallbackServer(_callbackServer);
    super.dispose();
  }
}
