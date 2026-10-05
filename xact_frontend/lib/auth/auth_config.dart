import 'package:flutter/foundation.dart';

import '../api/api_config.dart';
import 'auth_challenge.dart';

class AuthConfig {
  static const String _configuredAuthority = String.fromEnvironment(
    'KEYCLOAK_AUTHORITY',
  );

  /// keycloak runs next to the backend in the docker stack, so a phone that reaches
  /// the backend under a lan address reaches keycloak under the same one
  static String get authority {
    if (_configuredAuthority.isNotEmpty) {
      return _configuredAuthority;
    }

    final api = Uri.parse(ApiConfig.baseUrl);
    return Uri(
      scheme: api.scheme,
      host: api.host,
      port: 8080,
      path: '/realms/xact',
    ).toString();
  }

  static const String clientId = String.fromEnvironment(
    'KEYCLOAK_CLIENT_ID',
    defaultValue: 'x-act-frontend',
  );

  static const String _mobileRedirectUri = String.fromEnvironment(
    'KEYCLOAK_REDIRECT_URI',
    defaultValue: 'xact://login-callback',
  );

  // Desktop uses a localhost HTTP server — no custom URI scheme needed.
  static const int desktopCallbackPort = 9482;

  static bool get isDesktop =>
      !kIsWeb &&
      (defaultTargetPlatform == TargetPlatform.linux ||
          defaultTargetPlatform == TargetPlatform.windows ||
          defaultTargetPlatform == TargetPlatform.macOS);

  static String get redirectUri {
    if (kIsWeb) {
      // Redirect back to wherever the Flutter web app is currently running.
      return '${Uri.base.origin}/';
    }
    if (isDesktop) {
      return 'http://localhost:$desktopCallbackPort/auth/callback';
    }
    return _mobileRedirectUri;
  }

  static Uri endpoint(String name) {
    final base = authority.endsWith('/') ? authority : '$authority/';
    return Uri.parse('${base}protocol/openid-connect/$name');
  }

  static Uri loginUri(AuthChallenge challenge) {
    return endpoint('auth').replace(
      queryParameters: <String, String>{
        'client_id': clientId,
        'redirect_uri': redirectUri,
        'response_type': 'code',
        'scope': 'openid',
        // Always show the login form — prevents SSO silent re-auth after logout.
        'prompt': 'login',
        'code_challenge': challenge.challenge,
        'code_challenge_method': 'S256',
        'state': challenge.state,
      },
    );
  }
}
