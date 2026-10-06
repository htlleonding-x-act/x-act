namespace XActBackend.Core.Util;

public enum AwardType
{
    Marathon = 10,
    SpeedDemon = 20,
    Hunter = 30,
    Ghost = 40,
    TransitPro = 50,
    RuleBender = 60,
}

public sealed record AwardCandidate(
    int MemberId,
    double DistanceMeters,
    double TopSpeedKmh,
    int Catches,
    double LongestMrXStintSeconds,
    double TransitSeconds,
    double OutOfBoundsSeconds
);

/// <param name="Value">the winning metric in the unit of its candidate field</param>
public sealed record MatchAward(AwardType Type, IReadOnlyList<int> MemberIds, double Value);

public static class MatchAwardCalculator
{
    // below these nobody earned the award, a 50 m walk is no marathon
    private const double MarathonMinimumMeters = 500;
    private const double SpeedDemonMinimumKmh = 10;
    private const int HunterMinimumCatches = 1;
    private const double GhostMinimumSeconds = 5 * 60;
    private const double TransitProMinimumSeconds = 3 * 60;
    private const double RuleBenderMinimumSeconds = 30;

    /// <summary>
    ///     one award per category for the best member. members within the rounding step of the best value share
    ///     it, so gps noise doesn't decide a tie
    /// </summary>
    public static IReadOnlyList<MatchAward> Calculate(IReadOnlyList<AwardCandidate> candidates)
    {
        List<MatchAward> awards = [];
        if (candidates.Count == 0)
        {
            return awards;
        }

        AddAward(awards, candidates, AwardType.Marathon, c => c.DistanceMeters, MarathonMinimumMeters, tieStep: 10);
        AddAward(awards, candidates, AwardType.SpeedDemon, c => c.TopSpeedKmh, SpeedDemonMinimumKmh, tieStep: 0.1);
        AddAward(awards, candidates, AwardType.Hunter, c => c.Catches, HunterMinimumCatches, tieStep: 1);
        AddAward(awards, candidates, AwardType.Ghost, c => c.LongestMrXStintSeconds, GhostMinimumSeconds, tieStep: 1);
        AddAward(awards, candidates, AwardType.TransitPro, c => c.TransitSeconds, TransitProMinimumSeconds, tieStep: 1);
        AddAward(awards, candidates, AwardType.RuleBender, c => c.OutOfBoundsSeconds, RuleBenderMinimumSeconds, tieStep: 1);

        return awards;
    }

    private static void AddAward(
        List<MatchAward> awards,
        IReadOnlyList<AwardCandidate> candidates,
        AwardType type,
        Func<AwardCandidate, double> metric,
        double minimum,
        double tieStep)
    {
        double best = candidates.Max(metric);
        if (best < minimum)
        {
            return;
        }

        double bestKey = Math.Round(best / tieStep);
        List<int> winners = candidates
            .Where(c => Math.Round(metric(c) / tieStep) == bestKey)
            .Select(c => c.MemberId)
            .Order()
            .ToList();

        awards.Add(new MatchAward(type, winners, best));
    }
}
