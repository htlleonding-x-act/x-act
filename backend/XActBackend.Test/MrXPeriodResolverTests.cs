using AwesomeAssertions;
using NodaTime;
using XActBackend.Core.Util;

namespace XActBackend.Test;

public sealed class MrXPeriodResolverTests
{
    private static readonly Instant Start = Instant.FromUtc(2026, 1, 1, 12, 0);
    private static readonly Instant End = Start + Duration.FromHours(1);

    private static Instant At(int minutes) => Start + Duration.FromMinutes(minutes);

    [Fact]
    public void Resolve_ReturnsSinglePeriod_WhenNoCatches() =>
        MrXPeriodResolver.Resolve([], 1, Start, End).Should().Equal(new MrXPeriod(1, Start, End));

    [Fact]
    public void Resolve_ReturnsEmpty_WhenNoMrXTeamAndNoCatches() =>
        MrXPeriodResolver.Resolve([], null, Start, End).Should().BeEmpty();

    [Fact]
    public void Resolve_SplitsPeriodsAtEachCatch()
    {
        IReadOnlyList<MrXPeriod> periods = MrXPeriodResolver.Resolve(
            [new CatchRecord(At(40), 1, 2), new CatchRecord(At(20), 2, 1)], 1, Start, End);

        periods.Should().Equal(
            new MrXPeriod(1, Start, At(20)),
            new MrXPeriod(2, At(20), At(40)),
            new MrXPeriod(1, At(40), End));
    }

    [Fact]
    public void Resolve_UsesCaughtTeamOfFirstCatchAsInitialMrX() =>
        MrXPeriodResolver.Resolve([new CatchRecord(At(10), 3, 7)], 3, Start, End)[0].TeamId.Should().Be(7);

    [Fact]
    public void Resolve_IgnoresCatchesOutsideMatchWindow() =>
        MrXPeriodResolver.Resolve([new CatchRecord(At(90), 2, 1)], 2, Start, End)
            .Should().Equal(new MrXPeriod(2, Start, End));

    [Fact]
    public void MrXTeamAt_ReturnsCatchingTeam_AtCatchInstant()
    {
        IReadOnlyList<MrXPeriod> periods = MrXPeriodResolver.Resolve([new CatchRecord(At(20), 2, 1)], 2, Start, End);

        MrXPeriodResolver.MrXTeamAt(periods, At(10)).Should().Be(1);
        MrXPeriodResolver.MrXTeamAt(periods, At(20)).Should().Be(2);
    }

    [Fact]
    public void LongestStintSecondsByTeam_TakesLongestPeriodPerTeam()
    {
        IReadOnlyList<MrXPeriod> periods = MrXPeriodResolver.Resolve(
            [new CatchRecord(At(10), 2, 1), new CatchRecord(At(25), 1, 2)], 1, Start, End);

        IReadOnlyDictionary<int, double> stints = MrXPeriodResolver.LongestStintSecondsByTeam(periods);

        stints[1].Should().Be(35 * 60);
        stints[2].Should().Be(15 * 60);
    }

    [Fact]
    public void ResolveDetectiveColors_ReversesColorSwaps()
    {
        // team 1 started as mr.x in red, team 2 hunted in blue and caught it, so the colors swapped once
        Dictionary<int, string> finalColors = new() { [1] = "#0000FF", [2] = "#FF0000", [3] = "#00FF00" };

        IReadOnlyDictionary<int, string?> colors =
            MrXPeriodResolver.ResolveDetectiveColors(finalColors, [new CatchRecord(At(10), 2, 1)], 2);

        colors[1].Should().Be("#0000FF");
        colors[2].Should().Be("#0000FF");
        colors[3].Should().Be("#00FF00");
    }

    [Fact]
    public void ResolveDetectiveColors_ReturnsNull_ForTeamThatWasAlwaysMrX()
    {
        Dictionary<int, string> finalColors = new() { [1] = "#FF0000", [2] = "#0000FF" };

        IReadOnlyDictionary<int, string?> colors = MrXPeriodResolver.ResolveDetectiveColors(finalColors, [], 1);

        colors[1].Should().BeNull();
        colors[2].Should().Be("#0000FF");
    }
}
