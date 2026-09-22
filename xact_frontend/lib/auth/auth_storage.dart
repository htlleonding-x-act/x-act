import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'auth_challenge.dart';

class AuthStorage {
  AuthStorage._();

  static final FlutterSecureStorage _storage = const FlutterSecureStorage();

  static const _keyAccess = 'xact_access_token';
  static const _keyRefresh = 'xact_refresh_token';
  static const _keyId = 'xact_id_token';
  static const _keyVerifier = 'xact_pkce_verifier';
  static const _keyState = 'xact_auth_state';

  static Future<void> saveTokens({
    required String accessToken,
    String? refreshToken,
    String? idToken,
  }) async {
    await _storage.write(key: _keyAccess, value: accessToken);
    if (refreshToken != null) {
      await _storage.write(key: _keyRefresh, value: refreshToken);
    }
    if (idToken != null) {
      await _storage.write(key: _keyId, value: idToken);
    }
  }

  static Future<String?> loadAccessToken() async {
    return await _storage.read(key: _keyAccess);
  }

  static Future<String?> loadRefreshToken() async {
    return await _storage.read(key: _keyRefresh);
  }

  static Future<String?> loadIdToken() async {
    return await _storage.read(key: _keyId);
  }

  /// The challenge has to outlive the browser redirect, which on web reloads the
  /// whole app, so it cannot be held in memory.
  static Future<void> savePendingChallenge(AuthChallenge challenge) async {
    await _storage.write(key: _keyVerifier, value: challenge.verifier);
    await _storage.write(key: _keyState, value: challenge.state);
  }

  static Future<AuthChallenge?> loadPendingChallenge() async {
    final verifier = await _storage.read(key: _keyVerifier);
    final state = await _storage.read(key: _keyState);

    if (verifier == null || state == null) {
      return null;
    }

    return AuthChallenge(verifier: verifier, state: state);
  }

  static Future<void> clearPendingChallenge() async {
    await _storage.delete(key: _keyVerifier);
    await _storage.delete(key: _keyState);
  }

  static Future<void> clear() async {
    await _storage.delete(key: _keyAccess);
    await _storage.delete(key: _keyRefresh);
    await _storage.delete(key: _keyId);
    await clearPendingChallenge();
  }
}
