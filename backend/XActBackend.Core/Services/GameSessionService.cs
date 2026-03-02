using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IGameSessionService
{
    public ValueTask<IReadOnlyCollection<GameSession>> GetAllGameSessionsAsync(bool tracking);
    public ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByIdAsync(int sessionId, bool tracking);
    public ValueTask<OneOf<GameSession, Error>> AddGameSessionAsync(GameSessionData newGameSession);
    public ValueTask<OneOf<Success, NotFound>> UpdateGameSessionAsync(int sessionId, GameSessionData gameSessionData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteGameSessionAsync(int sessionId, bool tracking);
    public ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByJoinCodeAsync(string joinCode, bool tracking);

    public sealed record GameSessionData(
        int HostUserId,
        string JoinCode,
        SessionStatus Status = SessionStatus.Waiting,
        Instant? StartTime = null,
        Instant? EndTime = null,
        int PlannedDurationMinutes = 60,
        int MrXRevealInterval = 5
    );
}

internal sealed class GameSessionService(IUnitOfWork uow) : IGameSessionService
{

    public async ValueTask<IReadOnlyCollection<GameSession>> GetAllGameSessionsAsync(bool tracking)
    {
        IEnumerable<GameSession> gameSessions = await uow.GameSessionRepository.GetAllSessionsAsync(tracking);

        // TODO: avoid unnecessary copy, use IReadOnlyCollection<GameSession> directly
        return [.. gameSessions];
    }

    public async ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByIdAsync(int sessionId, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking);

        return gameSession is not null ? gameSession : new NotFound();
    }

    public async ValueTask<OneOf<GameSession, Error>> AddGameSessionAsync(IGameSessionService.GameSessionData newGameSession)
    {
        try
        {
            var gameSession = uow.GameSessionRepository.AddGameSession(
                newGameSession.HostUserId,
                newGameSession.JoinCode,
                newGameSession.PlannedDurationMinutes,
                newGameSession.MrXRevealInterval
            );

            gameSession.Status = newGameSession.Status;
            gameSession.StartTime = newGameSession.StartTime;
            gameSession.EndTime = newGameSession.EndTime;

            await uow.SaveChangesAsync();

            return gameSession;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateGameSessionAsync(int sessionId, IGameSessionService.GameSessionData gameSessionData, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking);

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

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteGameSessionAsync(int sessionId, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking);

        if (gameSession is null)
        {
            return new NotFound();
        }

        uow.GameSessionRepository.RemoveSession(gameSession);
        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByJoinCodeAsync(string joinCode, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByJoinCodeAsync(joinCode, tracking);

        return gameSession is not null ? gameSession : new NotFound();
    }
}
