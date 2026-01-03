using OneOf;
using OneOf.Types;

namespace XAct.Core.GameSessions;

public interface IGameSessionService
{
    public ValueTask<IReadOnlyCollection<GameSession>> GetAllGameSessionsAsync();
    public ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByIdAsync(int sessionId);
    public ValueTask<OneOf<GameSession, Error>> AddGameSessionAsync(GameSessionData newGameSession);
    public ValueTask<OneOf<Success, NotFound>> UpdateGameSessionAsync(int sessionId, GameSessionData gameSessionData);
    public ValueTask<OneOf<Success, NotFound>> DeleteGameSessionAsync(int sessionId);

    public sealed record GameSessionData(
        int HostUserId,
        string JoinCode,
        SessionStatus Status = SessionStatus.WAITING,
        Instant? StartTime = null,
        Instant? EndTime = null,
        int PlannedDurationMinutes = 60,
        int MrXRevealInterval = 5
    );
}

public sealed class GameSessionService(IDataStorage dataStorage) : IGameSessionService
{
    private static int _nextSessionId = 6;
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<GameSession>> GetAllGameSessionsAsync()
    {
        IEnumerable<GameSession> gameSessions = await _dataStorage.GetGameSessionsAsync();

        return [.. gameSessions];
    }

    public async ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByIdAsync(int sessionId)
    {
        var gameSession = await GetGameSessionById(sessionId);

        return gameSession is not null ? gameSession : new NotFound();
    }

    public async ValueTask<OneOf<GameSession, Error>> AddGameSessionAsync(IGameSessionService.GameSessionData newGameSession)
    {
        try
        {
            var gameSession = new GameSession
            {
                SessionId = _nextSessionId++,
                HostUserId = newGameSession.HostUserId,
                JoinCode = newGameSession.JoinCode,
                Status = newGameSession.Status,
                StartTime = newGameSession.StartTime,
                EndTime = newGameSession.EndTime,
                PlannedDurationMinutes = newGameSession.PlannedDurationMinutes,
                MrXRevealInterval = newGameSession.MrXRevealInterval
            };

            await _dataStorage.AddGameSessionAsync(gameSession);

            return gameSession;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateGameSessionAsync(int sessionId, IGameSessionService.GameSessionData gameSessionData)
    {
        var gameSession = await GetGameSessionById(sessionId);

        if (gameSession is null)
        {
            return new NotFound();
        }

        gameSession.HostUserId = gameSessionData.HostUserId;
        gameSession.JoinCode = gameSessionData.JoinCode;
        gameSession.Status = gameSessionData.Status;
        gameSession.StartTime = gameSessionData.StartTime;
        gameSession.EndTime = gameSessionData.EndTime;
        gameSession.PlannedDurationMinutes = gameSessionData.PlannedDurationMinutes;
        gameSession.MrXRevealInterval = gameSessionData.MrXRevealInterval;

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteGameSessionAsync(int sessionId)
    {
        var gameSession = await GetGameSessionById(sessionId);

        if (gameSession is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemoveGameSessionAsync(gameSession);

        return new Success();
    }

    private async ValueTask<GameSession?> GetGameSessionById(int sessionId)
    {
        IEnumerable<GameSession> gameSessions = await _dataStorage.GetGameSessionsAsync();

        return gameSessions.FirstOrDefault(gs => gs.SessionId == sessionId);
    }
}
