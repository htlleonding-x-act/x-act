import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:uni_links/uni_links.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/auth/auth_config.dart';
import 'package:xact_frontend/auth/local_callback_server.dart';
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
    WidgetsBinding.instance.addPostFrameCallback((_) => _launchKeycloak());
    _initCallbackListener();
  }

  // ── Callback listener setup ───────────────────────────────────────────────

  Future<void> _initCallbackListener() async {
    if (kIsWeb) {
      // Web: Keycloak redirects back to the same URL with ?code=...
      // Check current URL immediately (handles page-reload after redirect).
      _handleIncomingUri(Uri.base);
      return;
    }

    if (AuthConfig.isDesktop) {
      // Desktop: spin up a local HTTP server so no custom URI scheme is needed.
      _callbackServer = await startLocalCallbackServer(
        AuthConfig.desktopCallbackPort,
        _onCodeReceived,
      );
      return;
    }

    // Mobile (Android / iOS): receive via uni_links deep link.
    try {
      final initial = await getInitialUri();
      if (initial != null) _handleIncomingUri(initial);
    } catch (_) {}

    _deepLinkSub = uriLinkStream.listen(
      (uri) { if (uri != null) _handleIncomingUri(uri); },
      onError: (_) {},
    );
  }

  // ── Keycloak browser launch ───────────────────────────────────────────────

  Future<void> _launchKeycloak() async {
    if (_isLoading) return;
    _setLoading(true);

    // On web, open in a new tab — Keycloak will redirect back to this origin.
    // On desktop/mobile, open in an external application.
    final launched = await launchUrl(
      AuthConfig.loginUri,
      mode: kIsWeb ? LaunchMode.platformDefault : LaunchMode.externalApplication,
    );

    if (mounted && !launched) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Keycloak konnte nicht geöffnet werden.')),
      );
    }

    _setLoading(false);
  }

  // ── Code-exchange handler ─────────────────────────────────────────────────

  void _handleIncomingUri(Uri uri) {
    final code = uri.queryParameters['code'];
    if (code != null) _onCodeReceived(code);
  }

  Future<void> _onCodeReceived(String code) async {
    _setLoading(true);

    final success = await ApiService.instance.exchangeAuthCode(code);

    _setLoading(false);

    if (!mounted) return;

    if (success) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const StartScreen()),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Login fehlgeschlagen. Bitte erneut versuchen.')),
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
            'Anmelden mit Keycloak',
            style: XActText.title.copyWith(fontSize: 24),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 10),
          Text(
            _isLoading
                ? 'Warte auf Login-Antwort...'
                : 'Du wirst zum Keycloak-Login weitergeleitet.',
            style: XActText.body.copyWith(color: XActColors.text2, height: 1.45),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 22),
          if (_isLoading)
            const Center(child: CircularProgressIndicator())
          else
            XActBranding.buildPrimaryButton(
              text: 'Mit Keycloak anmelden',
              icon: Icons.login_rounded,
              onPressed: _launchKeycloak,
            ),
          const SizedBox(height: 16),
          Text(
            'Server: ${AuthConfig.authority}',
            style: XActText.caption.copyWith(color: XActColors.text3),
            textAlign: TextAlign.center,
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
