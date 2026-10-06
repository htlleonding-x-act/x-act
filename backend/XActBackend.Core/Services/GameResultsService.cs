using OneOf;
using OneOf.Types;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IGameResultsService
{
    /// <summary>everything the end screen shows. only finished sessions have results</summary>
    public ValueTask<OneOf<GameResults, NotFound, DomainError>> GetResultsAsync(int sessionId);

    public sealed record GameResults(
        GameSession Session,
        Instant Start,
        Instant End,
        int? WinnerTeamId,
        IReadOnlyList<TeamResult> Teams,
        IReadOnlyList<MemberResult> Members,
        IReadOnlyList<MrXPeriod> MrXPeriods,
        IReadOnlyList<TimelineEntry> Timeline,
        IReadOnlyList<MatchAward> Awards,
        IReadOnlyList<(double Latitude, double Longitude)> Geofence
    );

    /// <param name="DetectiveColorCode">the color the team hunted in, null if it was mr.x the whole match</param>
    public sealed record TeamResult(Team Team, string? DetectiveColorCode, int MemberCount, TeamTotals Totals);

    public sealed record TeamTotals(
        double DistanceMeters,
        double TopSpeedKmh,
        double MrXSeconds,
        int CatchesMade,
        int TimesCaught,
        int PowerUpsUsed,
        double OutOfBoundsSeconds
    );

    /// <param name="Route">downsampled for the replay</param>
    public sealed record MemberResult(TeamMember Member, string DisplayName, bool IsGuest, bool IsHost, MemberStats Stats, IReadOnlyList<TrackPoint> Route);

    public sealed record MemberStats(
        double DistanceMeters,
        double TopSpeedKmh,
        double? AvgMovingSpeedKmh,
        double? AvgPaceSecondsPerKm,
        double MovingSeconds,
        IReadOnlyDictionary<TransportMode, double> SecondsByMode,
        double OutOfBoundsSeconds,
        int OutOfBoundsCount,
        int PowerUpsUsed,
        double MrXSeconds,
        int RevealCount,
        int CatchesMade
    );
}

internal sealed class GameResultsService(IUnitOfWork uow, ILogger<GameResultsService> logger) : IGameResultsService
{
    private const double MetersPerSecondToKmh = 3.6;

    // pace over a few steps is noise
    private const double MinDistanceForPaceMeters = 200;

    // a position this old still says where a player stood when mr.x was caught
    private static readonly Duration PositionMaxAge = Duration.FromMinutes(2);

    private sealed record CreditedCatch(CatchRecord Catch, IReadOnlyList<int> MemberIds, (double Latitude, double Longitude)? Position);

    public async ValueTask<OneOf<IGameResultsService.GameResults, NotFound, DomainError>> GetResultsAsync(int sessionId)
    {
        GameSession? session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            return new NotFound();
        }

        if (session.Status != SessionStatus.Finished)
        {
            logger.LogWarning("Rejected results for session {SessionId} because it is in status {Status}", sessionId, session.Status);
            return DomainError.SessionNotFinished(sessionId, session.Status);
        }

        IReadOnlyCollection<Team> teams = await uow.TeamRepository.GetTeamsBySessionIdAsync(sessionId, tracking: false);
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionIdAsync(sessionId, tracking: false);
        List<string> userIds = members.Where(m => m.UserId is not null).Select(m => m.UserId!).Distinct().ToList();
        IReadOnlyCollection<User> users = userIds.Count > 0 ? await uow.UserRepository.GetUsersByIdsAsync(userIds, tracking: false) : [];
        IReadOnlyCollection<LocationLog> logs = await uow.LocationLogRepository.GetLogsBySessionIdAsync(sessionId, tracking: false);
        IReadOnlyCollection<CatchEvent> catchEvents = await uow.CatchEventRepository.GetCatchEventsBySessionIdAsync(sessionId, tracking: false);
        IReadOnlyCollection<Offense> offenses = await uow.OffenseRepository.GetOffensesBySessionAsync(sessionId, tracking: false);
        IReadOnlyCollection<PowerUpUsage> powerUps = await uow.PowerUpUsageRepository.GetUsagesBySessionIdAsync(sessionId, tracking: false);
        IReadOnlyCollection<GeofencePoint> geofence = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking: false);

        (Instant start, Instant end) = MatchWindow(session, logs);

        Dictionary<int, List<TrackPoint>> tracksByMember = logs
            .GroupBy(l => l.MemberId)
            .ToDictionary(g => g.Key, g => g.Select(ToTrackPoint)
                                             .Where(p => p.Timestamp >= start && p.Timestamp <= end)
                                             .OrderBy(p => p.Timestamp)
                                             .ToList());

        List<CatchRecord> catches = catchEvents.Select(c => new CatchRecord(c.OccurredAt, c.CatchingTeamId, c.CaughtTeamId)).ToList();
        int? finalMrXTeamId = teams.FirstOrDefault(t => t.Role == TeamRole.MrX)?.Id;
        int? winnerTeamId = teams.FirstOrDefault(t => t.Role == TeamRole.MrX && members.Any(m => m.TeamId == t.Id))?.Id;
        IReadOnlyList<MrXPeriod> periods = MrXPeriodResolver.Resolve(catches, finalMrXTeamId, start, end);
        IReadOnlyDictionary<int, double> mrXSecondsByTeam = MrXPeriodResolver.TotalSecondsByTeam(periods);
        List<CreditedCatch> creditedCatches = catches.Select(c => CreditCatch(c, members, tracksByMember)).ToList();

        Dictionary<string, User> usersById = users.ToDictionary(u => u.Id);
        List<IGameResultsService.MemberResult> memberResults = members
            .OrderBy(m => m.Id)
            .Select(m => BuildMemberResult(session, m, usersById.GetValueOrDefault(m.UserId ?? string.Empty),
                                           tracksByMember.GetValueOrDefault(m.Id) ?? [], start, end, offenses, powerUps,
                                           mrXSecondsByTeam, creditedCatches))
            .ToList();

        IReadOnlyDictionary<int, string?> detectiveColors =
            MrXPeriodResolver.ResolveDetectiveColors(teams.ToDictionary(t => t.Id, t => t.ColorCode), catches, finalMrXTeamId);
        List<IGameResultsService.TeamResult> teamResults = teams
            .OrderBy(t => t.Id)
            .Select(t => BuildTeamResult(t, detectiveColors.GetValueOrDefault(t.Id), memberResults, catches, mrXSecondsByTeam))
            .ToList();

        IReadOnlyList<TimelineEntry> timeline = BuildTimeline(session, start, end, members, tracksByMember, offenses, powerUps, creditedCatches);
        IReadOnlyList<MatchAward> awards = BuildAwards(teams, memberResults, periods);
        List<(double Latitude, double Longitude)> geofencePoints = geofence.Select(p => (p.Latitude, p.Longitude)).ToList();

        logger.LogInformation("Built results for session {SessionId} with {MemberCount} members and {CatchCount} catches",
                              sessionId, memberResults.Count, catches.Count);

        return new IGameResultsService.GameResults(session, start, end, winnerTeamId, teamResults, memberResults, periods,
                                                   timeline, awards, geofencePoints);
    }

    /// <summary>sessions finished before start and end were always stored fall back to their pings</summary>
    private static (Instant Start, Instant End) MatchWindow(GameSession session, IReadOnlyCollection<LocationLog> logs)
    {
        Instant start = session.StartTime ?? (logs.Count > 0 ? logs.Min(l => l.Timestamp) : session.CreatedAt);
        Instant end = session.EndTime ?? (logs.Count > 0 ? logs.Max(l => l.Timestamp) : start);

        return (start, end < start ? start : end);
    }

    private static TrackPoint ToTrackPoint(LocationLog log) =>
        new(log.Timestamp, log.Latitude, log.Longitude, log.AccuracyMeters, log.TransportMode, log.IsRevealedPosition);

    /// <summary>
    ///     mr.x only reports which team caught them, so the catch goes to the member of that team who stood closest
    ///     to mr.x. without positions the whole team gets it
    /// </summary>
    private static CreditedCatch CreditCatch(CatchRecord catchRecord, IReadOnlyCollection<TeamMember> members, Dictionary<int, List<TrackPoint>> tracksByMember)
    {
        List<(double Latitude, double Longitude)> caughtPositions = members
            .Where(m => m.TeamId == catchRecord.CaughtTeamId)
            .Select(m => PositionOf(m.Id, catchRecord.OccurredAt, tracksByMember))
            .OfType<(double Latitude, double Longitude)>()
            .ToList();

        List<TeamMember> catchingMembers = members.Where(m => m.TeamId == catchRecord.CatchingTeamId).ToList();
        (double Latitude, double Longitude)? caughtPosition = caughtPositions.Count > 0 ? caughtPositions[0] : null;

        var closest = catchingMembers
            .Select(m => (Member: m, Position: PositionOf(m.Id, catchRecord.OccurredAt, tracksByMember)))
            .Where(c => c.Position is not null && caughtPositions.Count > 0)
            .Select(c => (c.Member, Distance: caughtPositions.Min(p => GeoMath.HaversineMeters(p.Latitude, p.Longitude, c.Position!.Value.Latitude, c.Position.Value.Longitude))))
            .OrderBy(c => c.Distance)
            .FirstOrDefault();

        return closest.Member is not null
            ? new CreditedCatch(catchRecord, [closest.Member.Id], caughtPosition)
            : new CreditedCatch(catchRecord, catchingMembers.Select(m => m.Id).Order().ToList(), caughtPosition);
    }

    private static (double Latitude, double Longitude)? PositionOf(int memberId, Instant at, Dictionary<int, List<TrackPoint>> tracksByMember) =>
        tracksByMember.TryGetValue(memberId, out List<TrackPoint>? track) ? RouteStatsCalculator.PositionAt(track, at, PositionMaxAge) : null;

    private static IGameResultsService.MemberResult BuildMemberResult(
        GameSession session,
        TeamMember member,
        User? user,
        List<TrackPoint> track,
        Instant start,
        Instant end,
        IReadOnlyCollection<Offense> offenses,
        IReadOnlyCollection<PowerUpUsage> powerUps,
        IReadOnlyDictionary<int, double> mrXSecondsByTeam,
        List<CreditedCatch> creditedCatches)
    {
        RouteStats route = RouteStatsCalculator.Calculate(track, start, end);
        List<Offense> memberOffenses = offenses.Where(o => o.MemberId == member.Id && o.DetectedAt <= end).ToList();
        double outOfBoundsSeconds = memberOffenses.Sum(o => ClippedSeconds(o, start, end));
        bool hasPace = route.DistanceMeters >= MinDistanceForPaceMeters && route.MovingSeconds > 0;

        var stats = new IGameResultsService.MemberStats(
            route.DistanceMeters,
            route.TopSpeedMps * MetersPerSecondToKmh,
            hasPace ? route.DistanceMeters / route.MovingSeconds * MetersPerSecondToKmh : null,
            hasPace ? route.MovingSeconds / (route.DistanceMeters / 1000) : null,
            route.MovingSeconds,
            route.SecondsByMode,
            outOfBoundsSeconds,
            memberOffenses.Count,
            powerUps.Count(p => p.MemberId == member.Id),
            mrXSecondsByTeam.GetValueOrDefault(member.TeamId),
            track.Count(p => p.IsRevealed),
            creditedCatches.Count(c => c.MemberIds.Contains(member.Id)));

        bool isGuest = member.GuestName is not null || user?.IsGuest == true;
        bool isHost = member.UserId is not null && member.UserId == session.HostUserId;

        return new IGameResultsService.MemberResult(member, MemberDisplayName.Resolve(member, user), isGuest, isHost, stats,
                                                    RouteDownsampler.Downsample(track, route.AcceptedPoints));
    }

    private static double ClippedSeconds(Offense offense, Instant start, Instant end)
    {
        Instant from = offense.DetectedAt < start ? start : offense.DetectedAt;
        Instant to = offense.ClearedAt is { } cleared && cleared < end ? cleared : end;

        return Math.Max(0, (to - from).TotalSeconds);
    }

    private static IGameResultsService.TeamResult BuildTeamResult(
        Team team,
        string? detectiveColor,
        List<IGameResultsService.MemberResult> memberResults,
        List<CatchRecord> catches,
        IReadOnlyDictionary<int, double> mrXSecondsByTeam)
    {
        List<IGameResultsService.MemberStats> stats = memberResults.Where(m => m.Member.TeamId == team.Id).Select(m => m.Stats).ToList();

        var totals = new IGameResultsService.TeamTotals(
            stats.Sum(s => s.DistanceMeters),
            stats.Count > 0 ? stats.Max(s => s.TopSpeedKmh) : 0,
            mrXSecondsByTeam.GetValueOrDefault(team.Id),
            catches.Count(c => c.CatchingTeamId == team.Id),
            catches.Count(c => c.CaughtTeamId == team.Id),
            stats.Sum(s => s.PowerUpsUsed),
            stats.Sum(s => s.OutOfBoundsSeconds));

        return new IGameResultsService.TeamResult(team, detectiveColor, stats.Count, totals);
    }

    private static IReadOnlyList<TimelineEntry> BuildTimeline(
        GameSession session,
        Instant start,
        Instant end,
        IReadOnlyCollection<TeamMember> members,
        Dictionary<int, List<TrackPoint>> tracksByMember,
        IReadOnlyCollection<Offense> offenses,
        IReadOnlyCollection<PowerUpUsage> powerUps,
        List<CreditedCatch> creditedCatches)
    {
        Dictionary<int, TeamMember> membersById = members.ToDictionary(m => m.Id);

        List<RevealPing> reveals = tracksByMember
            .Where(t => membersById.ContainsKey(t.Key))
            .SelectMany(t => t.Value.Where(p => p.IsRevealed)
                              .Select(p => new RevealPing(t.Key, membersById[t.Key].TeamId, p.Timestamp, p.Latitude, p.Longitude)))
            .ToList();

        List<OffenseSpan> offenseSpans = offenses
            .Where(o => membersById.ContainsKey(o.MemberId))
            .Select(o =>
            {
                (double Latitude, double Longitude)? position = PositionOf(o.MemberId, o.DetectedAt, tracksByMember);
                return new OffenseSpan(o.MemberId, membersById[o.MemberId].TeamId, o.DetectedAt, o.ClearedAt,
                                       position?.Latitude, position?.Longitude);
            })
            .ToList();

        IEnumerable<TimelineEntry> catchEntries = creditedCatches.Select(c => new TimelineEntry(
            c.Catch.OccurredAt,
            TimelineEventType.MrXCaught,
            c.Catch.CatchingTeamId,
            c.MemberIds.Count == 1 ? c.MemberIds[0] : null,
            c.Catch.CaughtTeamId,
            c.Position?.Latitude,
            c.Position?.Longitude));

        IEnumerable<TimelineEntry> powerUpEntries = powerUps
            .Where(p => membersById.ContainsKey(p.MemberId))
            .Select(p =>
            {
                (double Latitude, double Longitude)? position = PositionOf(p.MemberId, p.UsedAt, tracksByMember);
                return new TimelineEntry(p.UsedAt, TimelineEventType.PowerUpUsed, membersById[p.MemberId].TeamId, p.MemberId,
                                         Latitude: position?.Latitude, Longitude: position?.Longitude, PowerUpType: p.PowerUpType);
            });

        return MatchTimelineBuilder.Build(start, end, session.MrXRevealInterval, reveals, offenseSpans,
                                          [.. catchEntries, .. powerUpEntries]);
    }

    private static IReadOnlyList<MatchAward> BuildAwards(
        IReadOnlyCollection<Team> teams,
        List<IGameResultsService.MemberResult> memberResults,
        IReadOnlyList<MrXPeriod> periods)
    {
        HashSet<int> spectatorTeamIds = teams.Where(t => t.Role == TeamRole.Spectator).Select(t => t.Id).ToHashSet();
        IReadOnlyDictionary<int, double> longestStintByTeam = MrXPeriodResolver.LongestStintSecondsByTeam(periods);

        List<AwardCandidate> candidates = memberResults
            .Where(m => !spectatorTeamIds.Contains(m.Member.TeamId))
            .Select(m => new AwardCandidate(
                m.Member.Id,
                m.Stats.DistanceMeters,
                m.Stats.TopSpeedKmh,
                m.Stats.CatchesMade,
                longestStintByTeam.GetValueOrDefault(m.Member.TeamId),
                m.Stats.SecondsByMode.Where(s => s.Key != TransportMode.Foot).Sum(s => s.Value),
                m.Stats.OutOfBoundsSeconds))
            .ToList();

        return MatchAwardCalculator.Calculate(candidates);
    }
}
