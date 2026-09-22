import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../api/api_service.dart';
import '../api/models.dart';
import 'app_session.dart';

/// ids of the android system notifications, one per chat
class _NotificationIds {
  static const int allChat = 90001;
  static const int teamChat = 90002;
}

/// tracks unread chats and shows or dismisses the android system notifications
/// so the user never sees stale alerts after reading a message
final class ChatNotificationService {
  ChatNotificationService._();

  static final ChatNotificationService instance = ChatNotificationService._();

  final FlutterLocalNotificationsPlugin _notifications =
      FlutterLocalNotificationsPlugin();

  StreamSubscription<RealtimeEventEnvelope>? _eventSubscription;

  bool _hasUnreadAll = false;
  bool _hasUnreadTeam = false;

  bool get hasUnreadAll => _hasUnreadAll;

  bool get hasUnreadTeam => _hasUnreadTeam;

  final StreamController<void> _changeController =
      StreamController<void>.broadcast();

  /// fires when [hasUnreadAll] or [hasUnreadTeam] changes
  Stream<void> get onChange => _changeController.stream;

  Future<void> init() async {
    // always listen so the in-app unread dots work on every platform, even
    // where system notifications don't exist
    await _eventSubscription?.cancel();
    _eventSubscription =
        ApiService.instance.realtimeEvents.listen(_onRealtimeEvent);

    // system notifications only exist for android so far
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) {
      return;
    }

    const androidSettings =
        AndroidInitializationSettings('@mipmap/ic_launcher');
    const initSettings = InitializationSettings(android: androidSettings);
    await _notifications.initialize(initSettings);

    await _notifications
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.requestNotificationsPermission();
  }

  /// also cancels every notification that is still showing
  void dispose() {
    unawaited(_eventSubscription?.cancel());
    _eventSubscription = null;

    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      unawaited(_notifications.cancelAll());
    }

    _hasUnreadAll = false;
    _hasUnreadTeam = false;
    _changeController.add(null);
  }

  /// hides both the in-app dot and the android notification
  void markAllChatRead() {
    if (!_hasUnreadAll) return;
    _hasUnreadAll = false;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      unawaited(_notifications.cancel(_NotificationIds.allChat));
    }
    _changeController.add(null);
  }

  /// hides both the in-app dot and the android notification
  void markTeamChatRead() {
    if (!_hasUnreadTeam) return;
    _hasUnreadTeam = false;
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      unawaited(_notifications.cancel(_NotificationIds.teamChat));
    }
    _changeController.add(null);
  }

  void _onRealtimeEvent(RealtimeEventEnvelope envelope) {
    if (envelope.type != RealtimeEvents.chatMessagePosted) return;

    final message = ChatMessage.fromJson(envelope.payload);

    final currentMemberId = AppSession.instance.currentMemberId;
    if (currentMemberId != null && message.senderMemberId == currentMemberId) {
      return;
    }

    if (message.isGlobal) {
      _setUnreadAll(message);
    } else if (message.teamId == AppSession.instance.currentTeamId) {
      _setUnreadTeam(message);
    }
  }

  void _setUnreadAll(ChatMessage message) {
    _hasUnreadAll = true;
    _changeController.add(null);
    unawaited(
      _showNotification(
        id: _NotificationIds.allChat,
        title: 'All Chat',
        body: '${message.senderName}: ${message.content}',
      ),
    );
  }

  void _setUnreadTeam(ChatMessage message) {
    _hasUnreadTeam = true;
    _changeController.add(null);
    unawaited(
      _showNotification(
        id: _NotificationIds.teamChat,
        title: 'Team Chat',
        body: '${message.senderName}: ${message.content}',
      ),
    );
  }

  Future<void> _showNotification({
    required int id,
    required String title,
    required String body,
  }) async {
    const androidDetails = AndroidNotificationDetails(
      'xact_chat',
      'Chat Messages',
      channelDescription: 'Notifications for new chat messages in X-ACT',
      importance: Importance.high,
      priority: Priority.high,
      playSound: true,
      enableVibration: true,
    );
    const details = NotificationDetails(android: androidDetails);

    try {
      await _notifications.show(id, title, body, details);
    } catch (e) {
      debugPrint('Failed to show notification: $e');
    }
  }
}
