part of 'api_service.dart';

extension ApiServiceReportMethods on ApiService {
  Future<KickVote?> loadOpenKickVote({int? sessionId}) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return null;
    }

    final json = await _getJsonObject(
      '/api/gamesessions/$resolvedSessionId/report/votes/open',
    );
    final vote = json['vote'];
    if (vote is! Map) {
      return null;
    }
    return KickVote.fromJson(vote.cast<String, dynamic>());
  }

  Future<List<MemberOffense>> loadActiveOffenses({int? sessionId}) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return const [];
    }

    final json = await _getJsonObject(
      '/api/gamesessions/$resolvedSessionId/report/offenses',
    );
    return ApiListResponse.fromJson(json, MemberOffense.fromJson).items;
  }

  Future<KickVote> startKickVote({
    required int targetMemberId,
    String? reason,
  }) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/report/votes',
      {
        'initiatorMemberId': memberId,
        'targetMemberId': targetMemberId,
        if (reason != null && reason.trim().isNotEmpty) 'reason': reason.trim(),
      },
    );
    return KickVote.fromJson(json);
  }

  Future<KickVote> castKickBallot({
    required int voteId,
    required bool approve,
  }) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/report/votes/$voteId/ballots',
      {'voterMemberId': memberId, 'approve': approve},
    );
    return KickVote.fromJson(json);
  }

  /// only the initiator or the host may cancel
  Future<KickVote> cancelKickVote({required int voteId}) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/report/votes/$voteId/cancel',
      {'actingMemberId': memberId},
    );
    return KickVote.fromJson(json);
  }

  /// kicks right away without a vote, host only
  Future<void> hostKickMember({
    required int targetMemberId,
    String? reason,
  }) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/report/kick',
      {
        'actingMemberId': memberId,
        'targetMemberId': targetMemberId,
        if (reason != null && reason.trim().isNotEmpty) 'reason': reason.trim(),
      },
    );
  }
}
