using AwesomeAssertions;
using NodaTime;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;

namespace XActBackend.Test;

public sealed class MatchTimelineBuilderTests
{
    private static readonly Instant Start = Instant.FromUtc(2026, 1, 1, 12, 0);
    private static readonly Instant End = Start + Duration.FromHours(1);

    private static Instant At(int minutes, int seconds = 0) => Start + Duration.FromMinutes(minutes) + Duration.FromSeconds(seconds);

    [Fact]
    public void Build_StartsWithGameStartedAndEndsWithGameEnded()
    {
        IReadOnlyList<TimelineEntry> timeline = MatchTimelineBuilder.Build(Start, End, 5, [], [],
            [new TimelineEntry(Start, TimelineEventType.PowerUpUsed, PowerUpType: PowerUpType.BlackTicket)]);

        timeline[0].Type.Should().Be(TimelineEventType.GameStarted);
        timeline[1].Type.Should().Be(TimelineEventType.PowerUpUsed);
        timeline[^1].Type.Should().Be(TimelineEventType.GameEnded);
    }

    [Fact]
    public void Build_GroupsRevealsPerWindow()
    {
        RevealPing[] reveals =
        [
            new(1, 1, At(0, 3), 48.30, 14.30),
            new(2, 1, At(0, 5), 48.31, 14.31),
            new(1, 1, At(5, 2), 48.32, 14.32),
        ];

        IReadOnlyList<TimelineEntry> timeline = MatchTimelineBuilder.Build(Start, End, 5, reveals, [], []);

        List<TimelineEntry> revealEntries = timeline.Where(e => e.Type == TimelineEventType.MrXRevealed).ToList();
        revealEntries.Should().HaveCount(2);
        revealEntries[0].MemberId.Should().Be(1);
        revealEntries[0].OccurredAt.Should().Be(At(0, 3));
    }

    [Fact]
    public void Build_UsesEndTime_ForOffenseNeverCleared()
    {
        IReadOnlyList<TimelineEntry> timeline = MatchTimelineBuilder.Build(Start, End, 5, [],
            [new OffenseSpan(1, 1, At(50), null, null, null)], []);

        timeline.Single(e => e.Type == TimelineEventType.LeftGameArea).DurationSeconds.Should().Be(600);
    }

    [Fact]
    public void Build_OrdersEventsChronologically()
    {
        IReadOnlyList<TimelineEntry> timeline = MatchTimelineBuilder.Build(Start, End, 5, [],
            [new OffenseSpan(1, 1, At(30), At(31), null, null)],
            [new TimelineEntry(At(10), TimelineEventType.MrXCaught, 2, OtherTeamId: 1)]);

        timeline.Select(e => e.OccurredAt).Should().BeInAscendingOrder();
        timeline.Select(e => e.Type).Should().Equal(
            TimelineEventType.GameStarted, TimelineEventType.MrXCaught, TimelineEventType.LeftGameArea, TimelineEventType.GameEnded);
    }
}
