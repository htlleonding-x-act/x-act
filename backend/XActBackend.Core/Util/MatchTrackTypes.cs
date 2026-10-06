using XActBackend.Persistence.Model;

namespace XActBackend.Core.Util;

public sealed record TrackPoint(
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode Mode,
    bool IsRevealed
);

public sealed record CatchRecord(Instant OccurredAt, int CatchingTeamId, int CaughtTeamId);

/// <summary>a stretch of the match in which one team was mr.x</summary>
public sealed record MrXPeriod(int TeamId, Instant From, Instant To);
