using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Provides methods to manage game sessions and their lifecycle.
/// </summary>
public interface IGameSessionService
{
    /// <summary>
    ///     Get all game sessions.
    /// </summary>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All game sessions</returns>
    public ValueTask<IReadOnlyCollection<GameSession>> GetAllGameSessionsAsync(bool tracking);

    /// <summary>
    ///     Get a game session by its id.
    /// </summary>
    /// <param name="sessionId">The id of the game session to find</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The game session, if found</returns>
    public ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByIdAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Add a new game session.
    /// </summary>
    /// <param name="newGameSession">The game session data to create</param>
    /// <returns>The created game session, or an error if the request is invalid</returns>
    public ValueTask<OneOf<GameSession, NotFound, DomainError>> AddGameSessionAsync(GameSessionData newGameSession);

    /// <summary>
    ///     Update an existing game session.
    /// </summary>
    /// <param name="sessionId">The id of the game session to update</param>
    /// <param name="gameSessionData">The new game session data</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the update was successful</returns>
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateGameSessionAsync(int sessionId, GameSessionData gameSessionData, bool tracking);

    /// <summary>
    ///     Delete a game session.
    /// </summary>
    /// <param name="sessionId">The id of the game session to delete</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the game session was deleted</returns>
    public ValueTask<OneOf<Success, NotFound>> DeleteGameSessionAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Get a game session by its join code.
    /// </summary>
    /// <param name="joinCode">The join code of the game session</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The game session, if found</returns>
    public ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByJoinCodeAsync(string joinCode, bool tracking);

    /// <summary>
    ///     Start a game session.
    /// </summary>
    /// <param name="sessionId">The id of the game session to start</param>
    /// <returns>Result indicating if the session could be started</returns>
    public ValueTask<OneOf<Success, NotFound, DomainError>> StartGameSessionAsync(int sessionId);

    /// <summary>
    ///     End a game session.
    /// </summary>
    /// <param name="sessionId">The id of the game session to end</param>
    /// <returns>Result indicating if the session could be ended</returns>
    public ValueTask<OneOf<Success, NotFound, DomainError>> EndGameSessionAsync(int sessionId);

    /// <summary>
    ///     Mark Mr.X as caught for a game session.
    /// </summary>
    /// <param name="sessionId">The id of the game session</param>
    /// <returns>Result indicating if Mr.X could be marked as caught</returns>
    public ValueTask<OneOf<Success, NotFound, DomainError>> CatchMrXAsync(int sessionId);

    /// <summary>
    ///     Data used to create or update a game session.
    /// </summary>
    /// <param name="HostUserId">The id of the host user</param>
    /// <param name="SessionName">The display name of the session</param>
    /// <param name="JoinCode">The join code used by participants</param>
    /// <param name="Status">The session status</param>
    /// <param name="StartTime">The optional actual start time</param>
    /// <param name="EndTime">The optional actual end time</param>
    /// <param name="PlannedDurationMinutes">The planned duration in minutes</param>
    /// <param name="MrXRevealInterval">The Mr.X reveal interval in minutes</param>
    public sealed record GameSessionData(
        int HostUserId,
        string SessionName,
        string JoinCode,
        SessionStatus Status = SessionStatus.Waiting,
        Instant? StartTime = null,
        Instant? EndTime = null,
        int PlannedDurationMinutes = 60,
        int MrXRevealInterval = 5
    );
}

internal sealed class GameSessionService(IUnitOfWork uow, IClock clock, ILogger<GameSessionService> logger) : IGameSessionService
{
    private const string HostTeamColor = "#000000";
    private const string DefaultMrXTeamName = "Team 1";


    public async ValueTask<IReadOnlyCollection<GameSession>> GetAllGameSessionsAsync(bool tracking)
        => await uow.GameSessionRepository.GetAllSessionsAsync(tracking);

    public async ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByIdAsync(int sessionId, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking);

        return gameSession is not null ? gameSession : new NotFound();
    }

    public async ValueTask<OneOf<GameSession, NotFound, DomainError>> AddGameSessionAsync(IGameSessionService.GameSessionData newGameSession)
    {
        try
        {
            var hostUser = await uow.UserRepository.GetUserByIdAsync(newGameSession.HostUserId, tracking: false);
            if (hostUser is null)
            {
                logger.LogWarning("Rejected session creation for missing host user {HostUserId}", newGameSession.HostUserId);
                return new NotFound();
            }

            if (hostUser.IsDeleted)
            {
                logger.LogWarning("Rejected session creation because host user {HostUserId} is deleted", newGameSession.HostUserId);
                return DomainError.HostUserDeleted(newGameSession.HostUserId);
            }

            var existingActiveSession = await uow.GameSessionRepository.GetActiveSessionByHostUserIdAsync(newGameSession.HostUserId, tracking: false);
            if (existingActiveSession is not null)
            {
                logger.LogWarning(
                    "Rejected session creation for host user {HostUserId} because session {ExistingSessionId} is still open",
                    newGameSession.HostUserId,
                    existingActiveSession.Id
                );
                return DomainError.HostUserAlreadyHasActiveSession(newGameSession.HostUserId);
            }

            var sessionWithJoinCode = await uow.GameSessionRepository.GetSessionByJoinCodeAsync(newGameSession.JoinCode, tracking: false);
            if (sessionWithJoinCode is not null)
            {
                logger.LogWarning("Rejected session creation because join code {JoinCode} is already in use", newGameSession.JoinCode);
                return DomainError.JoinCodeInUse(newGameSession.JoinCode);
            }

            var gameSession = uow.GameSessionRepository.AddGameSession(
                newGameSession.HostUserId,
                newGameSession.SessionName,
                newGameSession.JoinCode,
                newGameSession.PlannedDurationMinutes,
                newGameSession.MrXRevealInterval
            );

            gameSession.Status = newGameSession.Status;
            gameSession.StartTime = newGameSession.StartTime;
            gameSession.EndTime = newGameSession.EndTime;

            await uow.SaveChangesAsync();

            logger.LogInformation("Created game session {SessionId} with host user {HostUserId}", gameSession.Id, gameSession.HostUserId);

            var hostTeam = uow.TeamRepository.AddTeam(
                gameSession.Id,
                DefaultMrXTeamName,
                TeamRole.MrX,
                HostTeamColor,
                Team.DefaultMaxPlayerCount
            );

            await uow.SaveChangesAsync();

            logger.LogInformation("Created default host team {TeamId} for session {SessionId}", hostTeam.Id, gameSession.Id);

            uow.TeamMemberRepository.AddTeamMember(
                gameSession.Id,
                hostTeam.Id,
                newGameSession.HostUserId,
                guestName: null,
                isTeamLeader: true
            );

            await uow.SaveChangesAsync();

            logger.LogInformation("Created default host member for session {SessionId} and team {TeamId}", gameSession.Id, hostTeam.Id);

            return gameSession;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add game session {SessionName} for host {HostUserId}", newGameSession.SessionName, newGameSession.HostUserId);
            throw;
        }
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> UpdateGameSessionAsync(int sessionId, IGameSessionService.GameSessionData gameSessionData, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking);

        if (gameSession is null)
        {
            return new NotFound();
        }

        var hostUser = await uow.UserRepository.GetUserByIdAsync(gameSessionData.HostUserId, tracking: false);
        if (hostUser is null)
        {
            logger.LogWarning("Rejected update for session {SessionId} because host user {HostUserId} does not exist", sessionId, gameSessionData.HostUserId);
            return new NotFound();
        }

        if (hostUser.IsDeleted)
        {
            logger.LogWarning("Rejected update for session {SessionId} because host user {HostUserId} is deleted", sessionId, gameSessionData.HostUserId);
            return DomainError.HostUserDeleted(gameSessionData.HostUserId);
        }

        var conflictingSession = await uow.GameSessionRepository.GetActiveSessionByHostUserIdAsync(gameSessionData.HostUserId, tracking: false);
        if (conflictingSession is not null && conflictingSession.Id != sessionId)
        {
            logger.LogWarning(
                "Rejected update for session {SessionId} because host user {HostUserId} already owns open session {ExistingSessionId}",
                sessionId,
                gameSessionData.HostUserId,
                conflictingSession.Id
            );
            return DomainError.HostUserAlreadyHasActiveSession(gameSessionData.HostUserId);
        }

        var existingJoinCode = await uow.GameSessionRepository.GetSessionByJoinCodeExcludingIdAsync(gameSessionData.JoinCode, sessionId, tracking: false);
        if (existingJoinCode is not null)
        {
            logger.LogWarning("Rejected update for session {SessionId} because join code {JoinCode} is already in use", sessionId, gameSessionData.JoinCode);
            return DomainError.JoinCodeInUse(gameSessionData.JoinCode);
        }

        if (!IsValidStatusTransition(gameSession.Status, gameSessionData.Status))
        {
            logger.LogWarning(
                "Rejected update for session {SessionId} because status transition {CurrentStatus}->{RequestedStatus} is invalid",
                sessionId,
                gameSession.Status,
                gameSessionData.Status);
            return DomainError.InvalidSessionTransition(gameSession.Status, gameSessionData.Status);
        }

        gameSession.HostUserId = gameSessionData.HostUserId;
        gameSession.SessionName = gameSessionData.SessionName;
        gameSession.JoinCode = gameSessionData.JoinCode;
        gameSession.Status = gameSessionData.Status;
        gameSession.StartTime = gameSessionData.StartTime;
        gameSession.EndTime = gameSessionData.EndTime;
        gameSession.PlannedDurationMinutes = gameSessionData.PlannedDurationMinutes;
        gameSession.MrXRevealInterval = gameSessionData.MrXRevealInterval;

        await uow.SaveChangesAsync();

        logger.LogInformation("Updated game session {SessionId} to status {Status}", sessionId, gameSession.Status);

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

        logger.LogInformation("Deleted game session {SessionId}", sessionId);

        return new Success();
    }

    public async ValueTask<OneOf<GameSession, NotFound>> GetGameSessionByJoinCodeAsync(string joinCode, bool tracking)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByJoinCodeAsync(joinCode, tracking);

        return gameSession is not null ? gameSession : new NotFound();
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> StartGameSessionAsync(int sessionId)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: true);
        if (gameSession is null)
        {
            return new NotFound();
        }

        if (gameSession.Status != SessionStatus.Waiting)
        {
            logger.LogWarning("Rejected start for session {SessionId} because current status {Status} does not allow starting", sessionId, gameSession.Status);
            return DomainError.InvalidSessionTransition(gameSession.Status, SessionStatus.Active);
        }

        gameSession.Status = SessionStatus.Active;
        gameSession.StartTime = clock.GetCurrentInstant();

        await uow.SaveChangesAsync();

        logger.LogInformation("Started game session {SessionId}", sessionId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> EndGameSessionAsync(int sessionId)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: true);
        if (gameSession is null)
        {
            return new NotFound();
        }

        if (gameSession.Status != SessionStatus.Active)
        {
            logger.LogWarning("Rejected end for session {SessionId} because current status {Status} does not allow ending", sessionId, gameSession.Status);
            return DomainError.InvalidSessionTransition(gameSession.Status, SessionStatus.Finished);
        }

        gameSession.Status = SessionStatus.Finished;
        gameSession.EndTime = clock.GetCurrentInstant();

        await uow.SaveChangesAsync();

        logger.LogInformation("Ended game session {SessionId}", sessionId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> CatchMrXAsync(int sessionId)
    {
        var gameSession = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (gameSession is null)
        {
            return new NotFound();
        }

        if (gameSession.Status != SessionStatus.Active)
        {
            logger.LogWarning("Rejected catch for session {SessionId} because session is in status {Status}", sessionId, gameSession.Status);
            return DomainError.SessionNotActive(sessionId, gameSession.Status);
        }

        var mrXTeam = await uow.TeamRepository.GetTeamBySessionAndRoleAsync(sessionId, TeamRole.MrX, tracking: true);
        if (mrXTeam is null)
        {
            logger.LogWarning("Rejected catch for session {SessionId} because no MrX team was found", sessionId);
            return new NotFound();
        }

        mrXTeam.IsCaught = true;

        await uow.SaveChangesAsync();

        logger.LogInformation("MrX was caught in session {SessionId}, team {TeamId}", sessionId, mrXTeam.Id);

        return new Success();
    }

    private static bool IsValidStatusTransition(SessionStatus currentStatus, SessionStatus requestedStatus) =>
        currentStatus switch
        {
            SessionStatus.Waiting => requestedStatus is SessionStatus.Waiting or SessionStatus.Active,
            SessionStatus.Active => requestedStatus is SessionStatus.Active or SessionStatus.Finished,
            SessionStatus.Finished => requestedStatus == SessionStatus.Finished,
            _ => false,
        };
}
