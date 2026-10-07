part of 'api_service.dart';

extension ApiServiceProfileMethods on ApiService {
  Future<MyProfile> loadMyProfile() async {
    return MyProfile.fromJson(await _getJsonObject('/api/users/me'));
  }

  Future<void> renameMe(String username) async {
    final trimmed = username.trim();
    await _putJsonNoContent('/api/users/me', {'username': trimmed});

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
