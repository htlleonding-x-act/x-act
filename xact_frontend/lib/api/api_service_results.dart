part of 'api_service.dart';

extension ApiServiceResults on ApiService {
  /// a player can get the end event before the commit that finished the
  /// session is visible, so a 409 gets a few short retries
  Future<GameResults> loadGameResults(int sessionId) async {
    const attempts = 3;
    for (var attempt = 1; ; attempt++) {
      try {
        return GameResults.fromJson(
          await _getJsonObject('/api/gamesessions/$sessionId/results'),
        );
      } on ApiException catch (e) {
        if (e.statusCode != 409 || attempt >= attempts) rethrow;
        await Future<void>.delayed(const Duration(seconds: 1));
      }
    }
  }
}
