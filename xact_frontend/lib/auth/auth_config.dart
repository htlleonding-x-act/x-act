class AuthConfig {
  static const String authority = String.fromEnvironment(
    'KEYCLOAK_AUTHORITY',
    defaultValue: 'http://localhost:8080/realms/xact',
  );

  static const String clientId = String.fromEnvironment(
    'KEYCLOAK_CLIENT_ID',
    defaultValue: 'x-act-frontend',
  );

  static const String redirectUri = String.fromEnvironment(
    'KEYCLOAK_REDIRECT_URI',
    defaultValue: 'xact://login-callback',
  );

  static Uri get loginUri {
    final baseAuthority = authority.endsWith('/') ? authority : '$authority/';

    return Uri.parse('${baseAuthority}protocol/openid-connect/auth').replace(
      queryParameters: <String, String>{
        'client_id': clientId,
        'redirect_uri': redirectUri,
        'response_type': 'code',
        'scope': 'openid',
      },
    );
  }
}