part of 'api_service.dart';

extension ApiServiceHttpMethods on ApiService {
  Future<List<GeofencePointInfo>?> loadGeofencePoints(int sessionId) async {
    final json = await _getJsonObject('/api/gamesessions/$sessionId/geofencepoints');
    return ApiListResponse.fromJson(json, GeofencePointInfo.fromJson).items;
  }

  Future<List<UserInfo>> _listUsers() async {
    final json = await _getJsonObject('/api/users');
    return ApiListResponse.fromJson(json, UserInfo.fromJson).items;
  }

  Future<List<GameSessionInfo>> _listGameSessions() async {
    final json = await _getJsonObject('/api/gamesessions');
    return ApiListResponse.fromJson(json, GameSessionInfo.fromJson).items;
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

  Future<Map<String, dynamic>> _getJsonObject(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.get(
      uri,
      headers: {
        'Accept': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      },
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for GET $path');
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
    final response = await _http.post(
      uri,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      },
      body: jsonEncode(payload),
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
    final json = await _postJsonObject(path, payload);
    if (json == null) {
      throw Exception('POST $path failed');
    }
    return json;
  }

  Future<void> _postNoContent(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.post(
      uri,
      headers: {
        'Accept': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      },
    );
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for POST $path');
    }
  }

  Future<void> _postJsonNoContent(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.post(
      uri,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      },
      body: jsonEncode(payload),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for POST $path');
    }
  }

  Future<void> _putJsonNoContent(
    String path,
    Map<String, dynamic> payload,
  ) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.put(
      uri,
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      },
      body: jsonEncode(payload),
    );

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for PUT $path');
    }
  }

  Future<void> _deleteNoContent(String path) async {
    final uri = _baseUri.resolve(path);
    final response = await _http.delete(
      uri,
      headers: {
        'Accept': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      },
    );
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception('HTTP ${response.statusCode} for DELETE $path');
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

  Future<void> addLocationLog({
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
    await _postJsonObjectOrThrow(
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
  }
}