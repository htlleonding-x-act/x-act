part of 'api_service.dart';

extension ApiServiceReportMethods on ApiService {
  /// Load the session's single open kick vote, or `null` when none is running.
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

  /// Load the active offenses (flagged players) of the session.
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

  /// Start a kick vote against [targetMemberId] as the current member.
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

  /// Cast a ballot in an open kick vote as the current member.
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

  /// Cancel an open kick vote (allowed for the initiator or the host).
  Future<KickVote> cancelKickVote({required int voteId}) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/report/votes/$voteId/cancel',
      {'actingMemberId': memberId},
    );
    return KickVote.fromJson(json);
  }

  /// Instantly kick a member using host sudo powers.
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
