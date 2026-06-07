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

  bool get isAuthenticated => _accessToken != null;

  Future<bool> exchangeAuthCode(String code) async {
    // Public client: exchange directly with Keycloak — no backend proxy needed.
    final base = AuthConfig.authority.endsWith('/')
        ? AuthConfig.authority
        : '${AuthConfig.authority}/';
    final tokenUri = Uri.parse('${base}protocol/openid-connect/token');

    final resp = await _http.post(
      tokenUri,
      headers: {'Content-Type': 'application/x-www-form-urlencoded'},
      body: {
        'grant_type': 'authorization_code',
        'code': code,
        'redirect_uri': AuthConfig.redirectUri,
        'client_id': AuthConfig.clientId,
      },
    );

    if (resp.statusCode < 200 || resp.statusCode >= 300) return false;

    final decoded = jsonDecode(resp.body) as Map<String, dynamic>;
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

    try {
      await _syncUserWithBackend();
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
        final base = AuthConfig.authority.endsWith('/')
            ? AuthConfig.authority
            : '${AuthConfig.authority}/';
        await _http.post(
          Uri.parse('${base}protocol/openid-connect/logout'),
          headers: {'Content-Type': 'application/x-www-form-urlencoded'},
          body: {
            'client_id': AuthConfig.clientId,
            'refresh_token': refreshToken,
          },
        );
      }
    } catch (_) {}

    _accessToken = null;
    _session.currentUserId = null;
    _session.currentUsername = null;
    await AuthStorage.clear();
  }

  Stream<RealtimeEventEnvelope> get realtimeEvents => _realtime.eventStream;

  Stream<GameSessionSnapshot> get realtimeSnapshots => _realtime.snapshotStream;
}
