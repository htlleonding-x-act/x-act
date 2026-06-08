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
    public const string LocationLogRecorded = "location_log_recorded";
    public const string MrXCaught = "mr_x_caught";
    public const string ChatMessagePosted = "chat_message_posted";
    public const string RematchCreated = "rematch_created";
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
    IReadOnlyList<SnapshotLatestLocationDto> LatestLocations
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
