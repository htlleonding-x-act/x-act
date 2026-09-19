part of 'api_service.dart';

extension ApiServiceChatMethods on ApiService {
  Future<List<ChatMessage>> loadAllChatMessages({int? sessionId}) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return const [];
    }

    final json = await _getJsonObject(
      '/api/gamesessions/$resolvedSessionId/chat/all',
    );
    return ApiListResponse.fromJson(json, ChatMessage.fromJson).items;
  }

  Future<List<ChatMessage>> loadTeamChatMessages({
    required int teamId,
    int? sessionId,
  }) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return const [];
    }

    final json = await _getJsonObject(
      '/api/gamesessions/$resolvedSessionId/chat/teams/$teamId',
    );
    return ApiListResponse.fromJson(json, ChatMessage.fromJson).items;
  }

  Future<ChatMessage> sendAllChatMessage(String content) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/chat/all',
      {'senderMemberId': memberId, 'content': content},
    );
    return ChatMessage.fromJson(json);
  }

  Future<ChatMessage> sendTeamChatMessage({
    required int teamId,
    required String content,
  }) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/chat/teams/$teamId',
      {'senderMemberId': memberId, 'content': content},
    );
    return ChatMessage.fromJson(json);
  }

  /// all chat events only reach connections subscribed to the session group
  Future<void> ensureSessionChannelSubscription({int? sessionId}) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return;
    }

    await _ensureRealtimeSubscription(resolvedSessionId);
  }

  /// team chat events only reach connections that joined the team channel
  Future<void> ensureTeamChannelSubscription({
    required int teamId,
    int? sessionId,
  }) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return;
    }

    await _ensureRealtimeSubscription(resolvedSessionId);
    await _realtime.joinTeamChannel(
      sessionId: resolvedSessionId,
      teamId: teamId,
    );
  }

  Future<int> _requireSessionId() async {
    final sessionId = await getActiveSessionId();
    if (sessionId == null) {
      throw StateError('No active session found.');
    }
    return sessionId;
  }

  int _requireMemberId() {
    final memberId = _session.currentMemberId;
    if (memberId == null) {
      throw StateError('No active member identity found.');
    }
    return memberId;
  }
}
