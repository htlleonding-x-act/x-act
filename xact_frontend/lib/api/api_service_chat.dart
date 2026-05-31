part of 'api_service.dart';

extension ApiServiceChatMethods on ApiService {
  /// Load recent messages of the global "All" channel for the active session.
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

  /// Load recent messages of a team's private channel.
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

  /// Post a message to the global "All" channel as the current member.
  Future<ChatMessage> sendAllChatMessage(String content) async {
    final sessionId = await _requireSessionId();
    final memberId = _requireMemberId();

    final json = await _postJsonObjectOrThrow(
      '/api/gamesessions/$sessionId/chat/all',
      {'senderMemberId': memberId, 'content': content},
    );
    return ChatMessage.fromJson(json);
  }

  /// Post a message to the current member's team channel.
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

  /// Ensure the realtime connection is started and subscribed to the active
  /// session group, so global "All" chat events are delivered to this client.
  Future<void> ensureSessionChannelSubscription({int? sessionId}) async {
    final resolvedSessionId = sessionId ?? await getActiveSessionId();
    if (resolvedSessionId == null) {
      return;
    }

    await _ensureRealtimeSubscription(resolvedSessionId);
  }

  /// Ensure the realtime connection has joined the private team channel so
  /// team messages are delivered to this client.
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
