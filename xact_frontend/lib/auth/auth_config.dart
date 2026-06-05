import 'package:flutter/foundation.dart';

class AuthConfig {
  static const String authority = String.fromEnvironment(
    'KEYCLOAK_AUTHORITY',
    defaultValue: 'http://localhost:8080/realms/xact',
  );

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

  static Uri get loginUri {
    final base = authority.endsWith('/') ? authority : '$authority/';
    return Uri.parse('${base}protocol/openid-connect/auth').replace(
      queryParameters: <String, String>{
        'client_id': clientId,
        'redirect_uri': redirectUri,
        'response_type': 'code',
        'scope': 'openid',
      },
    );
  }
}
