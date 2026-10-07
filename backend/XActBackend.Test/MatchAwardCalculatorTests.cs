using AwesomeAssertions;
using XActBackend.Core.Util;

namespace XActBackend.Test;

public sealed class MatchAwardCalculatorTests
{
    private static AwardCandidate Candidate(
        int memberId,
        double distance = 0,
        double topSpeedKmh = 0,
        int catches = 0,
        double mrXStint = 0,
        double transit = 0,
        double outOfBounds = 0) =>
        new(memberId, distance, topSpeedKmh, catches, mrXStint, transit, outOfBounds);

    private static MatchAward? Award(IReadOnlyList<MatchAward> awards, AwardType type) =>
        awards.SingleOrDefault(a => a.Type == type);

    [Fact]
    public void Calculate_ReturnsNothing_ForNoCandidates() =>
        MatchAwardCalculator.Calculate([]).Should().BeEmpty();

    [Fact]
    public void Marathon_GoesToLongestDistance()
    {
        IReadOnlyList<MatchAward> awards = MatchAwardCalculator.Calculate([Candidate(1, distance: 800), Candidate(2, distance: 2400)]);

        Award(awards, AwardType.Marathon)!.MemberIds.Should().Equal(2);
        Award(awards, AwardType.Marathon)!.Value.Should().Be(2400);
    }

    [Fact]
    public void Marathon_NotAwarded_BelowMinimum() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, distance: 300)]), AwardType.Marathon).Should().BeNull();

    [Fact]
    public void Award_IsSharedByAllTiedMembers() =>
        Award(MatchAwardCalculator.Calculate([Candidate(3, distance: 1000), Candidate(1, distance: 1002), Candidate(2, distance: 700)]),
              AwardType.Marathon)!.MemberIds.Should().Equal(1, 3);

    [Fact]
    public void Award_IsShared_WhenValuesAreWithinTieStep_AcrossRoundingBoundary() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, topSpeedKmh: 12.06), Candidate(2, topSpeedKmh: 12.04)]), AwardType.SpeedDemon)!
            .MemberIds.Should().Equal(1, 2);

    [Fact]
    public void Award_IsNotShared_WhenValuesDifferByTieStep() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, topSpeedKmh: 12.2), Candidate(2, topSpeedKmh: 12.0)]), AwardType.SpeedDemon)!
            .MemberIds.Should().Equal(1);

    [Fact]
    public void SpeedDemon_RequiresMinimumSpeed() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, topSpeedKmh: 6)]), AwardType.SpeedDemon).Should().BeNull();

    [Fact]
    public void Hunter_GoesToMemberWithMostCatches() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, catches: 2), Candidate(2, catches: 1)]), AwardType.Hunter)!
            .MemberIds.Should().Equal(1);

    [Fact]
    public void Ghost_GoesToLongestStint() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, mrXStint: 600), Candidate(2, mrXStint: 1800)]), AwardType.Ghost)!
            .MemberIds.Should().Equal(2);

    [Fact]
    public void TransitPro_NotAwarded_WhenEveryoneOnFoot() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, distance: 2000), Candidate(2)]), AwardType.TransitPro).Should().BeNull();

    [Fact]
    public void RuleBender_GoesToLongestOutOfBoundsTime() =>
        Award(MatchAwardCalculator.Calculate([Candidate(1, outOfBounds: 45), Candidate(2, outOfBounds: 120)]), AwardType.RuleBender)!
            .MemberIds.Should().Equal(2);
}
