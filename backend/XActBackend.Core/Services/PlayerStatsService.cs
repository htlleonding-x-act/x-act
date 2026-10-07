using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IPlayerStatsService
{
    /// <summary>totals over every finished match the user is still a member of; a user without matches gets zeros</summary>
    public ValueTask<PlayerStats> GetStatsAsync(string userId);

    /// <summary>the user's finished matches, newest first</summary>
    public ValueTask<IReadOnlyList<MatchSummary>> GetMatchHistoryAsync(string userId, int limit);

    public sealed record PlayerStats(
        int GamesPlayed,
        int Wins,
        double DistanceMeters,
        double MrXSeconds,
        int CatchesMade,
        int PowerUpsUsed,
        double TopSpeedKmh
    );

    /// <param name="MemberId">the user's member in that match, so the client can show the match from their view</param>
    public sealed record MatchSummary(
        int SessionId,
        string SessionName,
        Instant Start,
        Instant End,
        int MemberId,
        string TeamName,
        bool Won,
        IGameResultsService.MemberStats Stats
    );
}

// builds on the end screen results, so the numbers always match what the player saw after each match
internal sealed class PlayerStatsService(
    IUnitOfWork uow,
    IGameResultsService gameResultsService,
    ILogger<PlayerStatsService> logger) : IPlayerStatsService
{
    public async ValueTask<IPlayerStatsService.PlayerStats> GetStatsAsync(string userId)
    {
        IReadOnlyList<IPlayerStatsService.MatchSummary> matches = await LoadMatchesAsync(userId, limit: null);

        return new IPlayerStatsService.PlayerStats(
            matches.Count,
            matches.Count(m => m.Won),
            matches.Sum(m => m.Stats.DistanceMeters),
            matches.Sum(m => m.Stats.MrXSeconds),
            matches.Sum(m => m.Stats.CatchesMade),
            matches.Sum(m => m.Stats.PowerUpsUsed),
            matches.Count > 0 ? matches.Max(m => m.Stats.TopSpeedKmh) : 0
        );
    }

    public ValueTask<IReadOnlyList<IPlayerStatsService.MatchSummary>> GetMatchHistoryAsync(string userId, int limit) =>
        LoadMatchesAsync(userId, limit);

    private async ValueTask<IReadOnlyList<IPlayerStatsService.MatchSummary>> LoadMatchesAsync(string userId, int? limit)
    {
        IReadOnlyList<int> sessionIds = await uow.TeamMemberRepository.GetFinishedSessionIdsOfUserAsync(userId, limit);
        var matches = new List<IPlayerStatsService.MatchSummary>(sessionIds.Count);

        foreach (int sessionId in sessionIds)
        {
            OneOf<IGameResultsService.GameResults, NotFound, DomainError> result =
                await gameResultsService.GetResultsAsync(sessionId);

            result.Switch(
                results =>
                {
                    if (ToSummary(results, userId) is { } summary)
                    {
                        matches.Add(summary);
                    }
                },
                notFound => logger.LogWarning("Finished session {SessionId} of user {UserId} has no results", sessionId, userId),
                domainError => logger.LogWarning("Skipped session {SessionId} of user {UserId}: {ErrorCode}", sessionId, userId, domainError.Code)
            );
        }

        return matches;
    }

    private static IPlayerStatsService.MatchSummary? ToSummary(IGameResultsService.GameResults results, string userId)
    {
        IGameResultsService.MemberResult? member = results.Members.FirstOrDefault(m => m.Member.UserId == userId);
        if (member is null)
        {
            return null;
        }

        int teamId = member.Member.TeamId;
        string teamName = results.Teams.FirstOrDefault(t => t.Team.Id == teamId)?.Team.TeamName ?? string.Empty;

        return new IPlayerStatsService.MatchSummary(
            results.Session.Id,
            results.Session.SessionName,
            results.Start,
            results.End,
            member.Member.Id,
            teamName,
            results.WinnerTeamId == teamId,
            member.Stats
        );
    }
}
