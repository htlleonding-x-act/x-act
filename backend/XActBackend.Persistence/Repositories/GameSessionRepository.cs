using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="GameSession"/> entities.
/// </summary>
public interface IGameSessionRepository
{
    /// <summary>
    ///     Add a new game session.
    /// </summary>
    /// <param name="hostUserId">The host user id</param>
    /// <param name="sessionName">The session name</param>
    /// <param name="joinCode">The join code</param>
    /// <param name="plannedDurationMinutes">The planned duration in minutes</param>
    /// <param name="mrXRevealInterval">The Mr.X reveal interval in minutes</param>
    /// <returns>The created game session entity</returns>
    public GameSession AddGameSession(
        string hostUserId,
        string sessionName,
        string joinCode,
        int plannedDurationMinutes,
        int mrXRevealInterval
    );

    /// <summary>
    ///     Get all game sessions.
    /// </summary>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All game sessions</returns>
    public ValueTask<IReadOnlyCollection<GameSession>> GetAllSessionsAsync(bool tracking);

    /// <summary>
    ///     Get a game session by its id.
    /// </summary>
    /// <param name="id">The id of the game session</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The game session, if found</returns>
    public ValueTask<GameSession?> GetSessionByIdAsync(int id, bool tracking);

    /// <summary>
    ///     Get a game session by join code.
    /// </summary>
    /// <param name="joinCode">The join code to search for</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The game session, if found</returns>
    public ValueTask<GameSession?> GetSessionByJoinCodeAsync(string joinCode, bool tracking);

    /// <summary>
    ///     Get a game session by join code while excluding one session id.
    /// </summary>
    /// <param name="joinCode">The join code to search for</param>
    /// <param name="excludedSessionId">Session id to exclude from lookup</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The matching game session, if found</returns>
    public ValueTask<GameSession?> GetSessionByJoinCodeExcludingIdAsync(string joinCode, int excludedSessionId, bool tracking);

    /// <summary>
    ///     Get an active (not finished) session by host user id.
    /// </summary>
    /// <param name="hostUserId">The host user id</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The active game session, if found</returns>
    public ValueTask<GameSession?> GetActiveSessionByHostUserIdAsync(string hostUserId, bool tracking);

    /// <summary>
    ///     Remove a game session from the repository.
    /// </summary>
    /// <param name="session">The game session to remove</param>
    public void RemoveSession(GameSession session);
}

internal sealed class GameSessionRepository(DbSet<GameSession> sessionSet) : IGameSessionRepository
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

    public void RemoveSession(GameSession session)
    {
        sessionSet.Remove(session);
    }
}
