using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions/{sessionId:int}/results")]
public sealed class GameResultsController(
    IGameResultsService gameResultsService,
    ILogger<GameResultsController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<GameResultsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<GameResultsDto>> GetGameResults([FromRoute] int sessionId)
    {
        OneOf<IGameResultsService.GameResults, NotFound, DomainError> result = await gameResultsService.GetResultsAsync(sessionId);

        return result.Match<ActionResult<GameResultsDto>>(
            results => Ok(GameResultsDto.FromResults(results)),
            notFound =>
            {
                logger.LogWarning("Results for game session {SessionId} not found", sessionId);
                return NotFound();
            },
            domainError => DomainErrorResult(domainError));
    }
}

/// <summary>every time inside the match is an offset in seconds from StartTime, which keeps routes small</summary>
public sealed record GameResultsDto(
    int SessionId,
    string SessionName,
    string HostUserId,
    Instant StartTime,
    Instant EndTime,
    int DurationSeconds,
    GameEndReason? EndReason,
    int? WinnerTeamId,
    int MrXRevealIntervalMinutes,
    IReadOnlyList<ResultTeamDto> Teams,
    IReadOnlyList<ResultMemberDto> Members,
    IReadOnlyList<MrXPeriodDto> MrXPeriods,
    IReadOnlyList<TimelineEventDto> Timeline,
    IReadOnlyList<AwardDto> Awards,
    IReadOnlyList<GeoPointDto> Geofence
)
{
    public static GameResultsDto FromResults(IGameResultsService.GameResults results)
    {
        int Offset(Instant at) => OffsetSeconds(results.Start, at);

        return new GameResultsDto(
            results.Session.Id,
            results.Session.SessionName,
            results.Session.HostUserId,
            results.Start,
            results.End,
            Offset(results.End),
            results.Session.EndReason,
            results.WinnerTeamId,
            results.Session.MrXRevealInterval,
            results.Teams.Select(ResultTeamDto.FromResult).ToList(),
            results.Members.Select(m => ResultMemberDto.FromResult(m, results.Start)).ToList(),
            results.MrXPeriods.Select(p => new MrXPeriodDto(p.TeamId, Offset(p.From), Offset(p.To))).ToList(),
            results.Timeline.Select(e => TimelineEventDto.FromEntry(e, results.Start)).ToList(),
            results.Awards.Select(a => new AwardDto(a.Type, a.MemberIds, Math.Round(a.Value, 2))).ToList(),
            results.Geofence.Select(p => new GeoPointDto(p.Latitude, p.Longitude)).ToList());
    }

    internal static int OffsetSeconds(Instant start, Instant at) => (int)Math.Round((at - start).TotalSeconds);

    // 6 decimals are about 10 cm, more only bloats the routes
    internal static double RoundCoordinate(double value) => Math.Round(value, 6);
}

public sealed record ResultTeamDto(
    int TeamId,
    string TeamName,
    TeamRole FinalRole,
    string ColorCode,
    string? DetectiveColorCode,
    int MemberCount,
    TeamTotalsDto Totals
)
{
    public static ResultTeamDto FromResult(IGameResultsService.TeamResult result) =>
        new(
            result.Team.Id,
            result.Team.TeamName,
            result.Team.Role,
            result.Team.ColorCode,
            result.DetectiveColorCode,
            result.MemberCount,
            new TeamTotalsDto(
                Math.Round(result.Totals.DistanceMeters, 1),
                Math.Round(result.Totals.TopSpeedKmh, 1),
                (int)Math.Round(result.Totals.MrXSeconds),
                result.Totals.CatchesMade,
                result.Totals.TimesCaught,
                result.Totals.PowerUpsUsed,
                (int)Math.Round(result.Totals.OutOfBoundsSeconds)));
}

public sealed record TeamTotalsDto(
    double DistanceMeters,
    double TopSpeedKmh,
    int MrXSeconds,
    int CatchesMade,
    int TimesCaught,
    int PowerUpsUsed,
    int OutOfBoundsSeconds
);

public sealed record ResultMemberDto(
    int MemberId,
    int TeamId,
    string DisplayName,
    bool IsGuest,
    bool IsHost,
    MemberStatsDto Stats,
    IReadOnlyList<RoutePointDto> Route
)
{
    public static ResultMemberDto FromResult(IGameResultsService.MemberResult result, Instant start)
    {
        IGameResultsService.MemberStats stats = result.Stats;

        var statsDto = new MemberStatsDto(
            Math.Round(stats.DistanceMeters, 1),
            Math.Round(stats.TopSpeedKmh, 1),
            stats.AvgMovingSpeedKmh is { } speed ? Math.Round(speed, 1) : null,
            stats.AvgPaceSecondsPerKm is { } pace ? Math.Round(pace) : null,
            (int)Math.Round(stats.MovingSeconds),
            stats.SecondsByMode.OrderBy(s => s.Key).Select(s => new TransportTimeDto(s.Key, (int)Math.Round(s.Value))).ToList(),
            (int)Math.Round(stats.OutOfBoundsSeconds),
            stats.OutOfBoundsCount,
            stats.PowerUpsUsed,
            (int)Math.Round(stats.MrXSeconds),
            stats.RevealCount,
            stats.CatchesMade);

        List<RoutePointDto> route = result.Route
            .Select(p => new RoutePointDto(
                GameResultsDto.OffsetSeconds(start, p.Timestamp),
                GameResultsDto.RoundCoordinate(p.Latitude),
                GameResultsDto.RoundCoordinate(p.Longitude),
                p.Mode,
                p.IsRevealed))
            .ToList();

        return new ResultMemberDto(result.Member.Id, result.Member.TeamId, result.DisplayName, result.IsGuest, result.IsHost,
                                   statsDto, route);
    }
}

public sealed record MemberStatsDto(
    double DistanceMeters,
    double TopSpeedKmh,
    double? AvgMovingSpeedKmh,
    double? AvgPaceSecondsPerKm,
    int MovingSeconds,
    IReadOnlyList<TransportTimeDto> TimeByTransportMode,
    int OutOfBoundsSeconds,
    int OutOfBoundsCount,
    int PowerUpsUsed,
    int MrXSeconds,
    int RevealCount,
    int CatchesMade
);

public sealed record TransportTimeDto(TransportMode Mode, int Seconds);

public sealed record RoutePointDto(int OffsetSeconds, double Latitude, double Longitude, TransportMode TransportMode, bool IsRevealed);

public sealed record MrXPeriodDto(int TeamId, int FromOffsetSeconds, int ToOffsetSeconds);

public sealed record TimelineEventDto(
    int OffsetSeconds,
    Instant OccurredAt,
    TimelineEventType Type,
    int? TeamId,
    int? MemberId,
    int? OtherTeamId,
    double? Latitude,
    double? Longitude,
    PowerUpType? PowerUpType,
    int? DurationSeconds
)
{
    public static TimelineEventDto FromEntry(TimelineEntry entry, Instant start) =>
        new(
            GameResultsDto.OffsetSeconds(start, entry.OccurredAt),
            entry.OccurredAt,
            entry.Type,
            entry.TeamId,
            entry.MemberId,
            entry.OtherTeamId,
            entry.Latitude is { } latitude ? GameResultsDto.RoundCoordinate(latitude) : null,
            entry.Longitude is { } longitude ? GameResultsDto.RoundCoordinate(longitude) : null,
            entry.PowerUpType,
            entry.DurationSeconds is { } seconds ? (int)Math.Round(seconds) : null);
}

public sealed record AwardDto(AwardType Type, IReadOnlyList<int> MemberIds, double Value);

public sealed record GeoPointDto(double Latitude, double Longitude);
