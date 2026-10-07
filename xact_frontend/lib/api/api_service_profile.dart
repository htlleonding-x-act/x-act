part of 'api_service.dart';

extension ApiServiceProfileMethods on ApiService {
  Future<MyProfile> loadMyProfile() async {
    return MyProfile.fromJson(await _getJsonObject('/api/users/me'));
  }

  /// replaces the whole profile, so pass the current avatar to keep it
  Future<void> updateMyProfile({
    required String username,
    required String? avatarEmoji,
    required String? avatarColor,
  }) async {
    final trimmed = username.trim();
    await _putJsonNoContent('/api/users/me', {
      'username': trimmed,
      'avatarEmoji': avatarEmoji,
      'avatarColor': avatarColor,
    });

    final userId = _session.currentUserId;
    if (userId == null) return;

    _session.setIdentity(userId: userId, username: trimmed);
    try {
      await AuthStorage.saveIdentity(userId: userId, username: trimmed);
    } catch (_) {}
  }

  /// the backend only flags the account, signing in again restores it
  Future<void> deleteMyAccount() async {
    await _deleteNoContent('/api/users/me');
    await logout();
  }
}
