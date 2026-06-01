import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthStorage {
  AuthStorage._();

  static final FlutterSecureStorage _storage = const FlutterSecureStorage();

  static const _keyAccess = 'xact_access_token';
  static const _keyRefresh = 'xact_refresh_token';

  static Future<void> saveTokens({required String accessToken, String? refreshToken}) async {
    await _storage.write(key: _keyAccess, value: accessToken);
    if (refreshToken != null) {
      await _storage.write(key: _keyRefresh, value: refreshToken);
    }
  }

  static Future<String?> loadAccessToken() async {
    return await _storage.read(key: _keyAccess);
  }

  static Future<String?> loadRefreshToken() async {
    return await _storage.read(key: _keyRefresh);
  }

  static Future<void> clear() async {
    await _storage.delete(key: _keyAccess);
    await _storage.delete(key: _keyRefresh);
  }
}
