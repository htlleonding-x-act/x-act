import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// the running match this device plays in, kept across app restarts so the
/// player can get back into it. the identity is stored too because a guest
/// has no other way to prove which member they are
typedef ActiveGame = ({
  int sessionId,
  String joinCode,
  int teamId,
  int memberId,
  bool isTeamLeader,
  String userId,
  String username,
});

class ActiveGameStorage {
  ActiveGameStorage._();

  static const FlutterSecureStorage _storage = FlutterSecureStorage();

  static const _keySessionId = 'xact_game_session_id';
  static const _keyJoinCode = 'xact_game_join_code';
  static const _keyTeamId = 'xact_game_team_id';
  static const _keyMemberId = 'xact_game_member_id';
  static const _keyTeamLeader = 'xact_game_team_leader';
  static const _keyUserId = 'xact_game_user_id';
  static const _keyUsername = 'xact_game_username';

  static Future<void> save(ActiveGame game) async {
    await _storage.write(key: _keySessionId, value: '${game.sessionId}');
    await _storage.write(key: _keyJoinCode, value: game.joinCode);
    await _storage.write(key: _keyTeamId, value: '${game.teamId}');
    await _storage.write(key: _keyMemberId, value: '${game.memberId}');
    await _storage.write(key: _keyTeamLeader, value: '${game.isTeamLeader}');
    await _storage.write(key: _keyUserId, value: game.userId);
    await _storage.write(key: _keyUsername, value: game.username);
  }

  static Future<ActiveGame?> load() async {
    final sessionId = int.tryParse(await _storage.read(key: _keySessionId) ?? '');
    final joinCode = await _storage.read(key: _keyJoinCode);
    final teamId = int.tryParse(await _storage.read(key: _keyTeamId) ?? '');
    final memberId = int.tryParse(await _storage.read(key: _keyMemberId) ?? '');
    final teamLeader = await _storage.read(key: _keyTeamLeader);
    final userId = await _storage.read(key: _keyUserId);
    final username = await _storage.read(key: _keyUsername);

    if (sessionId == null ||
        joinCode == null ||
        teamId == null ||
        memberId == null ||
        userId == null ||
        username == null) {
      return null;
    }

    return (
      sessionId: sessionId,
      joinCode: joinCode,
      teamId: teamId,
      memberId: memberId,
      isTeamLeader: teamLeader == 'true',
      userId: userId,
      username: username,
    );
  }

  static Future<void> clear() async {
    for (final key in [
      _keySessionId,
      _keyJoinCode,
      _keyTeamId,
      _keyMemberId,
      _keyTeamLeader,
      _keyUserId,
      _keyUsername,
    ]) {
      await _storage.delete(key: key);
    }
  }
}
