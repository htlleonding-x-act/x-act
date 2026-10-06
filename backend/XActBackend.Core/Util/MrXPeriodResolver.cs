namespace XActBackend.Core.Util;

public static class MrXPeriodResolver
{
    /// <summary>
    ///     splits the match at every catch. before the first catch its caught team was mr.x, after each catch its
    ///     catching team is. without catches the final mr.x team held the role the whole match
    /// </summary>
    public static IReadOnlyList<MrXPeriod> Resolve(IReadOnlyList<CatchRecord> catches, int? finalMrXTeamId, Instant start, Instant end)
    {
        List<CatchRecord> inMatch = catches
            .Where(c => c.OccurredAt >= start && c.OccurredAt <= end)
            .OrderBy(c => c.OccurredAt)
            .ToList();

        if (inMatch.Count == 0)
        {
            return finalMrXTeamId is { } teamId ? [new MrXPeriod(teamId, start, end)] : [];
        }

        List<MrXPeriod> periods = [];
        int mrXTeamId = inMatch[0].CaughtTeamId;
        Instant from = start;
        foreach (CatchRecord catchRecord in inMatch)
        {
            periods.Add(new MrXPeriod(mrXTeamId, from, catchRecord.OccurredAt));
            mrXTeamId = catchRecord.CatchingTeamId;
            from = catchRecord.OccurredAt;
        }

        periods.Add(new MrXPeriod(mrXTeamId, from, end));

        return periods;
    }

    public static int? MrXTeamAt(IReadOnlyList<MrXPeriod> periods, Instant at)
    {
        // the later period wins at a catch instant, the catching team is mr.x from then on
        MrXPeriod? period = periods.LastOrDefault(p => p.From <= at && at <= p.To);

        return period?.TeamId;
    }

    public static IReadOnlyDictionary<int, double> LongestStintSecondsByTeam(IReadOnlyList<MrXPeriod> periods) =>
        periods
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.Max(p => (p.To - p.From).TotalSeconds));

    public static IReadOnlyDictionary<int, double> TotalSecondsByTeam(IReadOnlyList<MrXPeriod> periods) =>
        periods
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.Sum(p => (p.To - p.From).TotalSeconds));

    /// <summary>
    ///     every catch swaps the colors of both teams, so the stored colors only show the final state. walks the
    ///     catches backwards to find the color each team had the last time it hunted. null for a team that was
    ///     mr.x the whole match
    /// </summary>
    public static IReadOnlyDictionary<int, string?> ResolveDetectiveColors(
        IReadOnlyDictionary<int, string> finalColorByTeam,
        IReadOnlyList<CatchRecord> catches,
        int? finalMrXTeamId)
    {
        List<CatchRecord> ordered = catches.OrderBy(c => c.OccurredAt).ToList();
        Dictionary<int, string> colors = new(finalColorByTeam);
        Dictionary<int, string?> detectiveColors = [];

        int? mrXTeamId = ordered.Count > 0 ? ordered[^1].CatchingTeamId : finalMrXTeamId;
        TakeDetectiveColors(colors, mrXTeamId, detectiveColors);

        for (int i = ordered.Count - 1; i >= 0; i--)
        {
            CatchRecord catchRecord = ordered[i];
            if (colors.TryGetValue(catchRecord.CatchingTeamId, out string? catchingColor)
                && colors.TryGetValue(catchRecord.CaughtTeamId, out string? caughtColor))
            {
                colors[catchRecord.CatchingTeamId] = caughtColor;
                colors[catchRecord.CaughtTeamId] = catchingColor;
            }

            TakeDetectiveColors(colors, catchRecord.CaughtTeamId, detectiveColors);
        }

        foreach (int teamId in finalColorByTeam.Keys)
        {
            detectiveColors.TryAdd(teamId, null);
        }

        return detectiveColors;
    }

    private static void TakeDetectiveColors(Dictionary<int, string> colors, int? mrXTeamId, Dictionary<int, string?> detectiveColors)
    {
        foreach ((int teamId, string color) in colors)
        {
            if (teamId != mrXTeamId)
            {
                detectiveColors.TryAdd(teamId, color);
            }
        }
    }
}
