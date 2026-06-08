using XActBackend.Core.Services;
using XActBackend.Persistence.Model;

namespace XActBackend.Core.Realtime;

public static class RealtimeMethods
{
    public const string Event = "realtime_event";
    public const string Snapshot = "realtime_snapshot";
}

public static class RealtimeEvents
{
    public const string TeamAdded = "team_added";
    public const string TeamUpdated = "team_updated";
    public const string TeamDeleted = "team_deleted";
    public const string TeamMemberJoined = "team_member_joined";
    public const string TeamMemberUpdated = "team_member_updated";
    public const string TeamMemberLeft = "team_member_left";
    public const string GameSessionStarted = "game_session_started";
    public const string GameSessionEnded = "game_session_ended";
    public const string LocationLogRecorded = "location_log_recorded";
    public const string MrXCaught = "mr_x_caught";
    public const string ChatMessagePosted = "chat_message_posted";
    public const string RematchCreated = "rematch_created";
    public const string KickVoteStarted = "kick_vote_started";
    public const string KickVoteUpdated = "kick_vote_updated";
    public const string KickVoteResolved = "kick_vote_resolved";
    public const string MemberKicked = "member_kicked";
    public const string MemberOffenseRaised = "member_offense_raised";
    public const string MemberOffenseCleared = "member_offense_cleared";
}

public static class KickReasons
{
    /// <summary>The member was removed because a kick vote passed.</summary>
    public const string Vote = "vote";

    /// <summary>The member was removed by the host using sudo powers.</summary>
    public const string Host = "host";
}

public static class RealtimeGroups
{
    public static string Session(int sessionId) => $"session:{sessionId}";

    // Private per-team channel; team chat is only delivered to connections that joined this group.
    public static string Team(int sessionId, int teamId) => $"session:{sessionId}:team:{teamId}";
}

public sealed record RealtimeEventEnvelope(string Type, object Payload);

public sealed record GameSessionSnapshot(
    int SessionId,
    string SessionName,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime,
    int PlannedDurationMinutes,
    int MrXRevealInterval,
    IReadOnlyList<SnapshotTeamDto> Teams,
    IReadOnlyList<SnapshotTeamMemberDto> Members,
    IReadOnlyList<SnapshotLatestLocationDto> LatestLocations,
    KickVotePayload? OpenKickVote,
    IReadOnlyList<MemberOffensePayload> ActiveOffenses
);

public sealed record SnapshotTeamDto(
    int Id,
    int SessionId,
    string TeamName,
    TeamRole Role,
    string ColorCode,
    bool IsCaught,
    int MaxPlayerCount
);

public sealed record TeamAddedPayload(
    int TeamId,
    int SessionId,
    string TeamName,
    TeamRole Role,
    string ColorCode,
    bool IsCaught,
    int MaxPlayerCount
);

public sealed record TeamUpdatedPayload(
    int TeamId,
    int SessionId,
    string TeamName,
    TeamRole Role,
    string ColorCode,
    bool IsCaught,
    int MaxPlayerCount
);

public sealed record TeamDeletedPayload(
    int TeamId,
    int SessionId
);

public sealed record SnapshotTeamMemberDto(
    int Id,
    int SessionId,
    int TeamId,
    int? UserId,
    string? GuestName,
    bool IsTeamLeader,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Instant? LastUpdated,
    Instant JoinedAt
);

public sealed record SnapshotLatestLocationDto(
    int LogId,
    int MemberId,
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode TransportMode,
    bool IsRevealedPosition
);

public sealed record TeamMemberJoinedPayload(
    int MemberId,
    int SessionId,
    int TeamId,
    int? UserId,
    string? GuestName,
    bool IsTeamLeader,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Instant? LastUpdated,
    Instant JoinedAt
);

public sealed record TeamMemberUpdatedPayload(
    int MemberId,
    int SessionId,
    int TeamId,
    int? UserId,
    string? GuestName,
    bool IsTeamLeader,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Instant? LastUpdated
);

public sealed record TeamMemberLeftPayload(
    int MemberId,
    int SessionId,
    int TeamId,
    int? UserId,
    string? GuestName,
    Instant LeftAt
);

public sealed record GameSessionStartedPayload(
    int SessionId,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime
);

public sealed record GameSessionEndedPayload(
    int SessionId,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime
);

public sealed record MrXCaughtPayload(
    int SessionId,
    int NewMrXTeamId,
    string NewMrXTeamName,
    int FormerMrXTeamId,
    string FormerMrXTeamName
);

public sealed record LocationLogRecordedPayload(
    int LogId,
    int SessionId,
    int TeamId,
    int MemberId,
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode TransportMode,
    bool IsRevealedPosition
);

public sealed record ChatMessagePostedPayload(
    int MessageId,
    int SessionId,
    int? TeamId,
    int? SenderMemberId,
    int? SenderTeamId,
    string SenderName,
    string Content,
    Instant SentAt
);

public sealed record RematchCreatedPayload(
    int FinishedSessionId,
    int NewSessionId,
    string NewJoinCode,
    string SessionName,
    int HostUserId
);

/// <summary>
///     A kick vote and its current tally. Doubles as the REST payload (open vote and action
///     responses) and the realtime payload for the started/updated/resolved events.
/// </summary>
public sealed record KickVotePayload(
    int VoteId,
    int SessionId,
    int? TargetMemberId,
    string TargetName,
    int? InitiatorMemberId,
    string InitiatorName,
    string? Reason,
    KickVoteStatus Status,
    int ApproveCount,
    int RejectCount,
    int EligibleVoterCount,
    Instant CreatedAt,
    Instant ExpiresAt,
    Instant? ResolvedAt
)
{
    public static KickVotePayload FromView(IReportService.KickVoteView view) =>
        new(
            view.VoteId,
            view.SessionId,
            view.TargetMemberId,
            view.TargetName,
            view.InitiatorMemberId,
            view.InitiatorName,
            view.Reason,
            view.Status,
            view.ApproveCount,
            view.RejectCount,
            view.EligibleVoterCount,
            view.CreatedAt,
            view.ExpiresAt,
            view.ResolvedAt);
}

public sealed record MemberKickedPayload(
    int SessionId,
    int TeamId,
    int MemberId,
    int? UserId,
    string? GuestName,
    string MemberName,
    string KickType,
    string? Reason,
    Instant KickedAt
);

/// <summary>
///     An automatically detected offense. Doubles as the REST payload (active offense list) and the
///     realtime payload for the raised/cleared events.
/// </summary>
public sealed record MemberOffensePayload(
    int OffenseId,
    int SessionId,
    int MemberId,
    OffenseType Type,
    OffenseStatus Status,
    Instant DetectedAt,
    Instant? ClearedAt
)
{
    public static MemberOffensePayload FromOffense(Offense offense) =>
        new(
            offense.Id,
            offense.SessionId,
            offense.MemberId,
            offense.Type,
            offense.Status,
            offense.DetectedAt,
            offense.ClearedAt);
}
