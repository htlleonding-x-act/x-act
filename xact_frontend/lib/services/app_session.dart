final class AppSession {
  AppSession._();

  static final AppSession instance = AppSession._();

  int? currentSessionId;
  String? currentJoinCode;

  int? currentUserId;
  String? currentUsername;

  int? currentTeamId;
  int? currentMemberId;
  bool isTeamLeader = false;

  void setSession({required int sessionId, required String joinCode}) {
    currentSessionId = sessionId;
    currentJoinCode = joinCode;
  }

  void setIdentity({required int userId, required String username}) {
    currentUserId = userId;
    currentUsername = username;
  }

  void setMembership({
    required int teamId,
    required int memberId,
    required bool teamLeader,
  }) {
    currentTeamId = teamId;
    currentMemberId = memberId;
    isTeamLeader = teamLeader;
  }

  void clearMembership() {
    currentTeamId = null;
    currentMemberId = null;
    isTeamLeader = false;
  }
}
