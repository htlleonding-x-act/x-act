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
    public const string SessionNotFinished = "session_not_finished";
    public const string MrXTeamAlreadyExists = "mr_x_team_already_exists";
    public const string CatchingTeamNotEligible = "catching_team_not_eligible";
    public const string TeamHasMembers = "team_has_members";
    public const string InvalidMemberIdentity = "invalid_member_identity";
    public const string TeamNotInSession = "team_not_in_session";
    public const string UserDeleted = "user_deleted";
    public const string UserAlreadyJoined = "user_already_joined";
    public const string TeamLeaderAlreadyExists = "team_leader_already_exists";
    public const string PowerUpNotAllowedForTeamRole = "power_up_not_allowed_for_team_role";
    public const string GeofencePointLimitReached = "geofence_point_limit_reached";
    public const string ChatNotTeamMember = "chat_not_team_member";
    public const string ReportTargetIsHost = "report_target_is_host";
    public const string ReportTargetIsSelf = "report_target_is_self";
    public const string ReportVoteAlreadyActive = "report_vote_already_active";
    public const string ReportAlreadyVoted = "report_already_voted";
    public const string ReportVoteNotOpen = "report_vote_not_open";
    public const string ReportNotHost = "report_not_host";
    public const string ReportCancelNotAllowed = "report_cancel_not_allowed";
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

    public static DomainError SessionNotFinished(int sessionId, SessionStatus status) =>
        new(DomainErrorCodes.SessionNotFinished,
            $"Session {sessionId} is in status {status} and cannot be used for a rematch until it has finished.");

    public static DomainError MrXTeamAlreadyExists(int sessionId) =>
        new(DomainErrorCodes.MrXTeamAlreadyExists, $"Session {sessionId} already has an Mr.X team.");

    public static DomainError CatchingTeamNotEligible(int teamId, TeamRole role) =>
        new(DomainErrorCodes.CatchingTeamNotEligible,
            $"Team {teamId} has role {role} and cannot catch Mr.X; only a detective team can.");

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

    public static DomainError GeofencePointLimitReached(int sessionId, int limit) =>
        new(DomainErrorCodes.GeofencePointLimitReached,
            $"Session {sessionId} has reached the maximum of {limit} geofence points.");

    public static DomainError ChatNotTeamMember(int memberId, int teamId) =>
        new(DomainErrorCodes.ChatNotTeamMember,
            $"Member {memberId} is not part of team {teamId} and cannot post to its chat channel.");

    public static DomainError ReportTargetIsHost(int memberId) =>
        new(DomainErrorCodes.ReportTargetIsHost, $"Member {memberId} is the host and cannot be kicked.");

    public static DomainError ReportTargetIsSelf(int memberId) =>
        new(DomainErrorCodes.ReportTargetIsSelf, $"Member {memberId} cannot start a kick vote against themselves.");

    public static DomainError ReportVoteAlreadyActive(int sessionId) =>
        new(DomainErrorCodes.ReportVoteAlreadyActive, $"Session {sessionId} already has an open kick vote.");

    public static DomainError ReportAlreadyVoted(int memberId, int voteId) =>
        new(DomainErrorCodes.ReportAlreadyVoted, $"Member {memberId} has already voted in kick vote {voteId}.");

    public static DomainError ReportVoteNotOpen(int voteId) =>
        new(DomainErrorCodes.ReportVoteNotOpen, $"Kick vote {voteId} is no longer open.");

    public static DomainError ReportNotHost(int memberId) =>
        new(DomainErrorCodes.ReportNotHost, $"Member {memberId} is not the host and cannot use host powers.");

    public static DomainError ReportCancelNotAllowed(int memberId, int voteId) =>
        new(DomainErrorCodes.ReportCancelNotAllowed,
            $"Member {memberId} may not cancel kick vote {voteId}; only the initiator or the host can.");
}
