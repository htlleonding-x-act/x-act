part of 'api_service.dart';

extension ApiServiceHttpMethods on ApiService {
  Future<List<GeofencePointInfo>?> loadGeofencePoints(int sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId/geofencepoints');
    return ApiListResponse.fromJson(json, GeofencePointInfo.fromJson).items;
  }

  Future<GameSessionDetails> _getGameSession(int sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId');
    return GameSessionDetails.fromJson(json);
  }

  Future<List<TeamDetails>> _listTeams(int sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId/teams');
    final items = ApiListResponse.fromJson(json, TeamInfo.fromJson).items;

    final details = await Future.wait(
      items.map((team) => _getTeam(sessionId, team.teamId)),
    );
    return details;
  }

  Future<TeamDetails> _getTeam(int sessionId, int teamId) async {
    final json = await _getJsonObject(
      '/api/gamesessions/$sessionId/teams/$teamId',
    );
    return TeamDetails.fromJson(json);
  }

  Future<List<TeamMemberInfo>> _listTeamMembersByTeam(
    int sessionId,
    int teamId,
  ) async {
    final json = await _getJsonObject(
      '/api/gamesessions/$sessionId/teams/$teamId/members',
    );
    return ApiListResponse.fromJson(json, TeamMemberInfo.fromJson).items;
  }

  Map<String, String> _headers({bool jsonBody = false}) => {
    'Accept': 'application/json',
    if (jsonBody) 'Content-Type': 'application/json',
    if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
  };

  /// Retries the request once with a refreshed access token, so an expired token
  /// does not surface as a failed call in the middle of a match.
  Future<http.Response> _send(Future<http.Response> Function() request) async {
    final response = await request();

    if (response.statusCode != 401 || !await _refreshAccessToken()) {
      return response;
    }

    return request();
  }

  Future<Map<String, dynamic>> _getJsonObject(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(() => _http.get(uri, headers: _headers()));

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(response.statusCode, _errorCode(response.body));
    }

    final decoded = jsonDecode(response.body);
    if (decoded is Map<String, dynamic>) {
      return decoded;
    }

    throw const FormatException('Expected JSON object response.');
  }

  Future<Map<String, dynamic>?> _postJsonObject(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(
      () => _http.post(uri, headers: _headers(jsonBody: true), body: jsonEncode(payload)),
    );

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) {
        return <String, dynamic>{};
      }
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic>) {
        return decoded;
      }
      throw const FormatException('Expected JSON object response.');
    }

    return null;
  }

  Future<Map<String, dynamic>> _postJsonObjectOrThrow(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(
      () => _http.post(uri, headers: _headers(jsonBody: true), body: jsonEncode(payload)),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(response.statusCode, _errorCode(response.body));
    }

    if (response.body.isEmpty) {
      return <String, dynamic>{};
    }
    final decoded = jsonDecode(response.body);
    if (decoded is Map<String, dynamic>) {
      return decoded;
    }
    throw const FormatException('Expected JSON object response.');
  }

  /// reads the `code` extension of a backend ProblemDetails body
  String? _errorCode(String body) {
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic> && decoded['code'] is String) {
        return decoded['code'] as String;
      }
    } on FormatException catch (_) {}
    return null;
  }

  Future<void> _postNoContent(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(() => _http.post(uri, headers: _headers()));
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(response.statusCode, _errorCode(response.body));
    }
  }

  Future<void> _postJsonNoContent(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(
      () => _http.post(uri, headers: _headers(jsonBody: true), body: jsonEncode(payload)),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(response.statusCode, _errorCode(response.body));
    }
  }

  Future<void> _putJsonNoContent(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(
      () => _http.put(uri, headers: _headers(jsonBody: true), body: jsonEncode(payload)),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(response.statusCode, _errorCode(response.body));
    }
  }

  Future<void> _deleteNoContent(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _send(() => _http.delete(uri, headers: _headers()));
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(response.statusCode, _errorCode(response.body));
    }
  }

  String _generateJoinCode() {
    const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    final random = Random();
    return List.generate(
      6,
      (_) => alphabet[random.nextInt(alphabet.length)],
    ).join();
  }

  String _roleToApi(TeamRole role) {
    return switch (role) {
      TeamRole.mrX => 'MrX',
      TeamRole.detective => 'Detective',
      TeamRole.spectator => 'Spectator',
    };
  }

  /// returns whether the server made this ping the reveal of mister x
  Future<bool> addLocationLog({
    required int sessionId,
    required int teamId,
    required int memberId,
    required DateTime timestamp,
    required double latitude,
    required double longitude,
    required double accuracyMeters,
    required String transportMode,
    bool isRevealedPosition = false,
  }) async {
    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/teams/$teamId/members/$memberId/locationlogs',
      {
        'timestamp': timestamp.toIso8601String(),
        'latitude': latitude,
        'longitude': longitude,
        'accuracyMeters': accuracyMeters,
        'transportMode': transportMode,
        'isRevealedPosition': isRevealedPosition,
      },
    );
    return json['isRevealedPosition'] == true;
  }
}