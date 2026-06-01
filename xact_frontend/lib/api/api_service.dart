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
    final uri = _baseUri.resolve('/auth/exchange');
    final body = jsonEncode({
      'code': code,
      'redirect_uri': AuthConfig.redirectUri,
    });

    final resp = await _http.post(
      uri,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
      body: body,
    );

    if (resp.statusCode < 200 || resp.statusCode >= 300) {
      return false;
    }

    final decoded = jsonDecode(resp.body) as Map<String, dynamic>;
    if (decoded.containsKey('access_token')) {
      _accessToken = decoded['access_token'] as String?;
      final refresh = decoded['refresh_token'] as String?;
      try {
        await AuthStorage.saveTokens(accessToken: _accessToken!, refreshToken: refresh);
      } catch (_) {}
      return true;
    }

    return false;
  }

  Future<void> loadStoredToken() async {
    final stored = await AuthStorage.loadAccessToken();
    if (stored != null) {
      _accessToken = stored;
    }
  }

  Future<void> logout() async {
    _accessToken = null;
    await AuthStorage.clear();
  }

  Stream<RealtimeEventEnvelope> get realtimeEvents => _realtime.eventStream;

  Stream<GameSessionSnapshot> get realtimeSnapshots => _realtime.snapshotStream;
}
