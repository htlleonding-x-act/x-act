import 'dart:convert';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';

import '../auth/auth_config.dart';
import '../auth/auth_storage.dart';
import '../services/app_session.dart';
import '../services/realtime_service.dart';
import 'api_config.dart';
import 'models.dart';

part 'api_service_types.dart';
part 'api_service_data.dart';
part 'api_service_session.dart';
part 'api_service_http.dart';
part 'api_service_chat.dart';
part 'api_service_report.dart';

final class ApiService {
  ApiService._({required String baseUrl})
    : _baseUri = Uri.parse(baseUrl),
      _http = http.Client();

  static final ApiService instance = ApiService._(baseUrl: ApiConfig.baseUrl);

  final Uri _baseUri;
  final http.Client _http;
  String? _accessToken;
  final AppSession _session = AppSession.instance;
  final RealtimeService _realtime = RealtimeService.instance;
  final Map<String, UserInfo> _usersById = {};

  bool get isAuthenticated => _accessToken != null;

  Future<bool> exchangeAuthCode(String code, String codeVerifier) async {
    // Public client: exchange directly with Keycloak — no backend proxy needed.
    final stored = await _requestTokens({
      'grant_type': 'authorization_code',
      'code': code,
      'redirect_uri': AuthConfig.redirectUri,
      'code_verifier': codeVerifier,
    });

    if (!stored) return false;

    try {
      await _syncUserWithBackend();
    } catch (_) {}

    return true;
  }

  /// Keycloak access tokens expire after a few minutes, so a running session has to
  /// trade its refresh token for a new one instead of falling back to a guest user.
  Future<bool> _refreshAccessToken() async {
    final refreshToken = await AuthStorage.loadRefreshToken();
    if (refreshToken == null) return false;

    final refreshed = await _requestTokens({
      'grant_type': 'refresh_token',
      'refresh_token': refreshToken,
    });

    if (!refreshed) await _clearTokens();

    return refreshed;
  }

  Future<bool> _requestTokens(Map<String, String> grant) async {
    final response = await _http.post(
      AuthConfig.endpoint('token'),
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},
      body: {...grant, 'client_id': AuthConfig.clientId},
    );

    if (response.statusCode < 200 || response.statusCode >= 300) return false;

    final decoded = jsonDecode(response.body) as Map<String, dynamic>;
    final accessToken = decoded['access_token'] as String?;
    if (accessToken == null) return false;

    _accessToken = accessToken;
    try {
      await AuthStorage.saveTokens(
        accessToken: accessToken,
        refreshToken: decoded['refresh_token'] as String?,
        idToken: decoded['id_token'] as String?,
      );
    } catch (_) {}

    return true;
  }

  Future<void> loadStoredToken() async {
    final stored = await AuthStorage.loadAccessToken();
    if (stored != null) {
      _accessToken = stored;
    }
  }

  Future<void> logout() async {
    // Back-channel logout: tell Keycloak to end the SSO session server-side.
    // This requires the refresh token and does not need a browser redirect.
    try {
      final refreshToken = await AuthStorage.loadRefreshToken();
      if (refreshToken != null) {
        await _http.post(
          AuthConfig.endpoint('logout'),
          headers: {'Content-Type': 'application/x-www-form-urlencoded'},
          body: {
            'client_id': AuthConfig.clientId,
            'refresh_token': refreshToken,
          },
        );
      }
    } catch (_) {}

    _session.currentUserId = null;
    _session.currentUsername = null;
    await _clearTokens();
  }

  Future<void> _clearTokens() async {
    _accessToken = null;
    await AuthStorage.clear();
  }

  Stream<RealtimeEventEnvelope> get realtimeEvents => _realtime.eventStream;

  Stream<GameSessionSnapshot> get realtimeSnapshots => _realtime.snapshotStream;
}
