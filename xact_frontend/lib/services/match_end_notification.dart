import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

/// warns a player with the app in the background that the match ends soon.
/// the plugin is a singleton that ChatNotificationService.init already set up
abstract final class MatchEndNotification {
  // next to the chat notification ids 90001 and 90002
  static const int _id = 90003;

  static Future<void> show(String text) async {
    // system notifications only exist for android so far
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) return;

    const details = NotificationDetails(
      android: AndroidNotificationDetails(
        'xact_match',
        'Match',
        channelDescription: 'Warnings before a match in X-ACT ends',
        importance: Importance.high,
        priority: Priority.high,
        playSound: true,
        enableVibration: true,
      ),
    );

    try {
      await FlutterLocalNotificationsPlugin().show(_id, 'X-ACT', text, details);
    } catch (e) {
      debugPrint('Failed to show notification: $e');
    }
  }
}
