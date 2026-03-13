using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IGameSessionRepository
{
    public GameSession AddGameSession(
        int hostUserId,
        string sessionName,
        string joinCode,
        int plannedDurationMinutes,
        int mrXRevealInterval
    );
    public ValueTask<IReadOnlyCollection<GameSession>> GetAllSessionsAsync(bool tracking);
    public ValueTask<GameSession?> GetSessionByIdAsync(int id, bool tracking);
    public ValueTask<GameSession?> GetSessionByJoinCodeAsync(string joinCode, bool tracking);
    public ValueTask<GameSession?> GetActiveSessionByHostUserIdAsync(int hostUserId, bool tracking);
    public void RemoveSession(GameSession session);
}

internal sealed class GameSessionRepository(DbSet<GameSession> sessionSet) : IGameSessionRepository
{
    private IQueryable<GameSession> Sessions => sessionSet;
    private IQueryable<GameSession> SessionsNoTracking => Sessions.AsNoTracking();

    public GameSession AddGameSession(
        int hostUserId,
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
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
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

    public async ValueTask<GameSession?> GetActiveSessionByHostUserIdAsync(int hostUserId, bool tracking)
    {
        IQueryable<GameSession> source = tracking ? Sessions : SessionsNoTracking;

        return await source.FirstOrDefaultAsync(
            s => s.HostUserId == hostUserId && s.Status != SessionStatus.Finished
        );
    }

    public void RemoveSession(GameSession session)
    {
        sessionSet.Remove(session);
    }
}
