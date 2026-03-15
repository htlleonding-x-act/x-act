using XActBackend.Persistence.Model;

namespace XActBackend.Core.Services;

public static class DomainErrorCodes
{
    public const string HostUserDeleted = "host_user_deleted";
    public const string HostUserAlreadyHasActiveSession = "host_user_already_has_active_session";
    public const string JoinCodeInUse = "join_code_in_use";
    public const string InvalidSessionTransition = "invalid_session_transition";
    public const string SessionNotJoinable = "session_not_joinable";
    public const string SessionNotActive = "session_not_active";
    public const string MrXTeamAlreadyExists = "mr_x_team_already_exists";
    public const string TeamHasMembers = "team_has_members";
    public const string InvalidMemberIdentity = "invalid_member_identity";
    public const string TeamNotInSession = "team_not_in_session";
    public const string UserDeleted = "user_deleted";
    public const string UserAlreadyJoined = "user_already_joined";
    public const string TeamLeaderAlreadyExists = "team_leader_already_exists";
    public const string PowerUpNotAllowedForTeamRole = "power_up_not_allowed_for_team_role";
}

public sealed record DomainError(string Code, string Message)
{
    public static DomainError HostUserDeleted(int userId) =>
        new(DomainErrorCodes.HostUserDeleted, $"Host user {userId} is deleted and cannot host a session.");

    public static DomainError HostUserAlreadyHasActiveSession(int userId) =>
        new(DomainErrorCodes.HostUserAlreadyHasActiveSession, $"Host user {userId} already has an open session.");

    public static DomainError JoinCodeInUse(string joinCode) =>
        new(DomainErrorCodes.JoinCodeInUse, $"Join code '{joinCode}' is already in use.");

    public static DomainError InvalidSessionTransition(SessionStatus fromStatus, SessionStatus toStatus) =>
        new(DomainErrorCodes.InvalidSessionTransition,
            $"Session status cannot change from {fromStatus} to {toStatus}.");

    public static DomainError SessionNotJoinable(int sessionId, SessionStatus status) =>
        new(DomainErrorCodes.SessionNotJoinable,
            $"Session {sessionId} is in status {status} and no longer accepts team or member changes.");

    public static DomainError SessionNotActive(int sessionId, SessionStatus status) =>
        new(DomainErrorCodes.SessionNotActive,
            $"Session {sessionId} is in status {status} and does not accept gameplay events.");

    public static DomainError MrXTeamAlreadyExists(int sessionId) =>
        new(DomainErrorCodes.MrXTeamAlreadyExists, $"Session {sessionId} already has an Mr.X team.");

    public static DomainError TeamHasMembers(int teamId) =>
        new(DomainErrorCodes.TeamHasMembers, $"Team {teamId} still has members and cannot be deleted.");

    public static DomainError InvalidMemberIdentity() =>
        new(DomainErrorCodes.InvalidMemberIdentity,
            "A team member must reference either a registered user or a guest name, but not both.");

    public static DomainError TeamNotInSession(int teamId, int sessionId) =>
        new(DomainErrorCodes.TeamNotInSession, $"Team {teamId} does not belong to session {sessionId}.");

    public static DomainError UserDeleted(int userId) =>
        new(DomainErrorCodes.UserDeleted, $"User {userId} is deleted and cannot join a session.");

    public static DomainError UserAlreadyJoined(int userId, int sessionId) =>
        new(DomainErrorCodes.UserAlreadyJoined, $"User {userId} is already a member of session {sessionId}.");

    public static DomainError TeamLeaderAlreadyExists(int teamId) =>
        new(DomainErrorCodes.TeamLeaderAlreadyExists, $"Team {teamId} already has a team leader.");

    public static DomainError PowerUpNotAllowedForTeamRole(PowerUpType powerUpType, TeamRole teamRole) =>
        new(DomainErrorCodes.PowerUpNotAllowedForTeamRole,
            $"Power-up {powerUpType} is not allowed for team role {teamRole}.");
}
