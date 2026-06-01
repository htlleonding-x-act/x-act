import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import 'dart:async';
import 'package:uni_links/uni_links.dart';
import 'package:xact_frontend/api/api_service.dart';
import 'package:xact_frontend/auth/auth_config.dart';
import 'package:xact_frontend/screens/start/start_screen.dart';
import 'package:xact_frontend/widgets/xact_branding.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  bool _isLaunching = false;
  StreamSubscription? _sub;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _launchKeycloak());
    _initUniLinks();
  }

  Future<void> _initUniLinks() async {
    // handle app already opened via link
    try {
      final initial = await getInitialUri();
      if (initial != null) {
        _handleIncomingUri(initial);
      }
    } catch (_) {}

    _sub = uriLinkStream.listen((uri) {
      if (uri != null) _handleIncomingUri(uri);
    }, onError: (_) {});
  }

  Future<void> _handleIncomingUri(Uri uri) async {
    final code = uri.queryParameters['code'];
    if (code == null) return;

    setState(() {
      _isLaunching = true;
    });

    final success = await ApiService.instance.exchangeAuthCode(code);

    setState(() {
      _isLaunching = false;
    });

    if (success && mounted) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const StartScreen()),
      );
    } else if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Login fehlgeschlagen')),
      );
    }
  }

  Future<void> _launchKeycloak() async {
    if (_isLaunching) {
      return;
    }

    setState(() {
      _isLaunching = true;
    });

    final bool launched = await launchUrl(
      AuthConfig.loginUri,
      mode: LaunchMode.externalApplication,
    );

    if (!mounted) {
      return;
    }

    if (!launched) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Keycloak konnte nicht geöffnet werden.'),
        ),
      );
    }

    setState(() {
      _isLaunching = false;
    });
  }

  void _enterApp() {
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (_) => const StartScreen()),
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
                      Container(
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
                              'Du wirst zum Keycloak-Login weitergeleitet, damit du dich mit deinem Account anmelden kannst.',
                              style: XActText.body.copyWith(
                                color: XActColors.text2,
                                height: 1.45,
                              ),
                              textAlign: TextAlign.center,
                            ),
                            const SizedBox(height: 22),
                            XActBranding.buildPrimaryButton(
                              text: _isLaunching ? 'Öffne Login...' : 'Mit Keycloak anmelden',
                              icon: Icons.login_rounded,
                              onPressed: _isLaunching ? null : _launchKeycloak,
                            ),
                            const SizedBox(height: 12),
                            XActBranding.buildGhostButton(
                              text: 'Zur App wechseln',
                              icon: Icons.arrow_forward_rounded,
                              onPressed: _enterApp,
                            ),
                            const SizedBox(height: 16),
                            Text(
                              'Login-URL: ${AuthConfig.authority}',
                              style: XActText.caption.copyWith(
                                color: XActColors.text3,
                              ),
                              textAlign: TextAlign.center,
                            ),
                          ],
                        ),
                      ),
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

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }
}