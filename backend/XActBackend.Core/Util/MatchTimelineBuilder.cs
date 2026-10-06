using XActBackend.Persistence.Model;

namespace XActBackend.Core.Util;

public enum TimelineEventType
{
    GameStarted = 10,
    MrXRevealed = 20,
    MrXCaught = 30,
    PowerUpUsed = 40,
    LeftGameArea = 50,
    GameEnded = 60,
}

public sealed record TimelineEntry(
    Instant OccurredAt,
    TimelineEventType Type,
    int? TeamId = null,
    int? MemberId = null,
    int? OtherTeamId = null,
    double? Latitude = null,
    double? Longitude = null,
    PowerUpType? PowerUpType = null,
    double? DurationSeconds = null
);

public sealed record RevealPing(int MemberId, int TeamId, Instant Timestamp, double Latitude, double Longitude);

public sealed record OffenseSpan(int MemberId, int TeamId, Instant DetectedAt, Instant? ClearedAt, double? Latitude, double? Longitude);

public static class MatchTimelineBuilder
{
    /// <param name="otherEntries">catches and power-ups, which need no grouping</param>
    public static IReadOnlyList<TimelineEntry> Build(
        Instant start,
        Instant end,
        int revealIntervalMinutes,
        IReadOnlyList<RevealPing> reveals,
        IReadOnlyList<OffenseSpan> offenses,
        IReadOnlyList<TimelineEntry> otherEntries)
    {
        List<TimelineEntry> entries =
        [
            new(start, TimelineEventType.GameStarted),
            new(end, TimelineEventType.GameEnded),
        ];

        entries.AddRange(RevealEntries(start, end, revealIntervalMinutes, reveals));
        entries.AddRange(OffenseEntries(start, end, offenses));
        entries.AddRange(otherEntries.Where(e => e.OccurredAt >= start && e.OccurredAt <= end));

        return entries
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => SortRank(e.Type))
            .ThenBy(e => e.Type)
            .ToList();
    }

    // every member of the mr.x team is revealed in the same window, the feed shows that as one reveal
    private static IEnumerable<TimelineEntry> RevealEntries(Instant start, Instant end, int revealIntervalMinutes, IReadOnlyList<RevealPing> reveals) =>
        reveals
            .Where(r => r.Timestamp >= start && r.Timestamp <= end)
            .GroupBy(r => (r.TeamId, Window: RevealWindowStart(start, r.Timestamp, revealIntervalMinutes)))
            .Select(g => g.MinBy(r => r.Timestamp)!)
            .Select(r => new TimelineEntry(r.Timestamp, TimelineEventType.MrXRevealed, r.TeamId, r.MemberId,
                                           Latitude: r.Latitude, Longitude: r.Longitude));

    private static Instant RevealWindowStart(Instant start, Instant at, int revealIntervalMinutes) =>
        RevealTimingCalculator.TryGetRevealWindow(start, at, revealIntervalMinutes, out Instant windowStart, out _, out _, out _)
            ? windowStart
            : at;

    private static IEnumerable<TimelineEntry> OffenseEntries(Instant start, Instant end, IReadOnlyList<OffenseSpan> offenses) =>
        offenses
            .Where(o => o.DetectedAt <= end)
            .Select(o =>
            {
                Instant from = o.DetectedAt < start ? start : o.DetectedAt;
                Instant to = o.ClearedAt is { } cleared && cleared < end ? cleared : end;
                double seconds = Math.Max(0, (to - from).TotalSeconds);

                return new TimelineEntry(from, TimelineEventType.LeftGameArea, o.TeamId, o.MemberId,
                                         Latitude: o.Latitude, Longitude: o.Longitude, DurationSeconds: seconds);
            });

    private static int SortRank(TimelineEventType type) =>
        type switch
        {
            TimelineEventType.GameStarted => 0,
            TimelineEventType.GameEnded => 2,
            _ => 1,
        };
}
