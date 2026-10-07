import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// settings that only live on this device, so guests have them too. the
/// notifiers let screens and services react the moment a switch flips
final class PreferencesService {
  PreferencesService._();

  static final PreferencesService instance = PreferencesService._();

  static const FlutterSecureStorage _storage = FlutterSecureStorage();

  static const _keyChatNotifications = 'xact_pref_chat_notifications';
  static const _keyTeamChatOnly = 'xact_pref_team_chat_only';
  static const _keyHapticFeedback = 'xact_pref_haptic_feedback';

  final ValueNotifier<bool> chatNotifications = ValueNotifier(true);
  final ValueNotifier<bool> teamChatOnly = ValueNotifier(false);
  final ValueNotifier<bool> hapticFeedback = ValueNotifier(true);

  /// keeps the defaults when the storage can't be read, e.g. on linux
  /// without a keyring, instead of keeping the app from starting
  Future<void> load() async {
    try {
      chatNotifications.value =
          await _readBool(_keyChatNotifications) ?? chatNotifications.value;
      teamChatOnly.value =
          await _readBool(_keyTeamChatOnly) ?? teamChatOnly.value;
      hapticFeedback.value =
          await _readBool(_keyHapticFeedback) ?? hapticFeedback.value;
    } catch (_) {}
  }

  Future<void> setChatNotifications(bool value) =>
      _write(chatNotifications, _keyChatNotifications, value);

  Future<void> setTeamChatOnly(bool value) =>
      _write(teamChatOnly, _keyTeamChatOnly, value);

  Future<void> setHapticFeedback(bool value) =>
      _write(hapticFeedback, _keyHapticFeedback, value);

  Future<bool?> _readBool(String key) async {
    return switch (await _storage.read(key: key)) {
      'true' => true,
      'false' => false,
      _ => null,
    };
  }

  /// the switch flips right away, even if saving fails it holds for this run
  Future<void> _write(
    ValueNotifier<bool> notifier,
    String key,
    bool value,
  ) async {
    notifier.value = value;
    try {
      await _storage.write(key: key, value: '$value');
    } catch (_) {}
  }
}
