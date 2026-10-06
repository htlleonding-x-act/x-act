using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IGameSessionRepository
{
    public GameSession AddGameSession(
        string hostUserId,
        string sessionName,
        string joinCode,
        int plannedDurationMinutes,
        int mrXRevealInterval
    );

    public ValueTask<IReadOnlyCollection<GameSession>> GetAllSessionsAsync(bool tracking);

    public ValueTask<GameSession?> GetSessionByIdAsync(int id, bool tracking);

    public ValueTask<GameSession?> GetSessionByJoinCodeAsync(string joinCode, bool tracking);

    public ValueTask<GameSession?> GetSessionByJoinCodeExcludingIdAsync(string joinCode, int excludedSessionId, bool tracking);

    /// <summary>active here means not finished yet, so a waiting session counts too</summary>
    public ValueTask<GameSession?> GetActiveSessionByHostUserIdAsync(string hostUserId, bool tracking);

    /// <summary>
    ///     sessions nobody can use anymore: running ones without any member and waiting ones whose host is gone. only
    ///     sessions created before <paramref name="createdBefore"/> count, so a lobby still being set up stays
    /// </summary>
    public ValueTask<IReadOnlyCollection<GameSession>> GetAbandonedSessionsAsync(Instant createdBefore);

    public void RemoveSession(GameSession session);
}

internal sealed class GameSessionRepository(DbSet<GameSession> sessionSet, IClock clock) : IGameSessionRepository
{
    private IQueryable<GameSession> Sessions => sessionSet;
    private IQueryable<GameSession> SessionsNoTracking => Sessions.AsNoTracking();

    public GameSession AddGameSession(
        string hostUserId,
        string sessionName,
        string joinCode,
        int plannedDurationMinutes,
        int mrXRevealInterval
    )
    {
        var session = new GameSession
        {
            HostUserId = hostUserId,
            SessionName = sessionName,
            JoinCode = joinCode,
            Status = SessionStatus.Waiting,
            PlannedDurationMinutes = plannedDurationMinutes,
            MrXRevealInterval = mrXRevealInterval,
            CreatedAt = clock.GetCurrentInstant(),
        };

        sessionSet.Add(session);

        return session;
    }

    public async ValueTask<IReadOnlyCollection<GameSession>> GetAllSessionsAsync(bool tracking)
    {
        IQueryable<GameSession> source = tracking ? Sessions : SessionsNoTracking;

        List<GameSession> sessions = await source.ToListAsync();

        return sessions;
    }

    public async ValueTask<GameSession?> GetSessionByIdAsync(int id, bool tracking)
    {
        IQueryable<GameSession> source = tracking ? Sessions : SessionsNoTracking;

        return await source.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async ValueTask<GameSession?> GetSessionByJoinCodeAsync(string joinCode, bool tracking)
    {
        IQueryable<GameSession> source = tracking ? Sessions : SessionsNoTracking;

        return await source.FirstOrDefaultAsync(s => s.JoinCode == joinCode);
    }

    public async ValueTask<GameSession?> GetSessionByJoinCodeExcludingIdAsync(string joinCode, int excludedSessionId, bool tracking)
    {
        IQueryable<GameSession> source = tracking ? Sessions : SessionsNoTracking;

        return await source.FirstOrDefaultAsync(s => s.JoinCode == joinCode && s.Id != excludedSessionId);
    }

    public async ValueTask<GameSession?> GetActiveSessionByHostUserIdAsync(string hostUserId, bool tracking)
    {
        IQueryable<GameSession> source = tracking ? Sessions : SessionsNoTracking;

        return await source.FirstOrDefaultAsync(
            s => s.HostUserId == hostUserId && s.Status != SessionStatus.Finished
        );
    }

    public async ValueTask<IReadOnlyCollection<GameSession>> GetAbandonedSessionsAsync(Instant createdBefore)
    {
        List<GameSession> sessions = await Sessions
            .Where(s => s.CreatedAt < createdBefore)
            .Where(s => (s.Status == SessionStatus.Active && !s.Teams.Any(t => t.Members.Any()))
                        || (s.Status == SessionStatus.Waiting
                            && !s.Teams.Any(t => t.Members.Any(m => m.UserId == s.HostUserId))))
            .ToListAsync();

        return sessions;
    }

    public void RemoveSession(GameSession session)
    {
        sessionSet.Remove(session);
    }
}
