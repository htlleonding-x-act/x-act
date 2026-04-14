using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Util;
using XActBackend.Core.Services;
using XActBackend.Core.Realtime;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions")]
public sealed class GameSessionController(
    ITransactionProvider transaction,
    IGameSessionService gameSessionService,
    IGameSessionRealtimePublisher realtimePublisher,
    IClock clock,
    ILogger<GameSessionController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<GameSessionListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<GameSessionListResponse>> GetAllGameSessions()
    {
        IReadOnlyCollection<GameSession> gameSessions = await gameSessionService.GetAllGameSessionsAsync(tracking: false);

        return Ok(new GameSessionListResponse
        {
            Items = gameSessions.Select(GameSessionInformationDto.FromGameSession).ToList()
        });
    }

    [HttpGet]
    [Route("{sessionId:int}")]
    [ProducesResponseType<GameSessionDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<GameSessionDetailsDto>> GetGameSessionById([FromRoute] int sessionId)
    {
        OneOf<GameSession, NotFound> sessionResult = await gameSessionService.GetGameSessionByIdAsync(sessionId, tracking: false);

        return sessionResult.Match<ActionResult<GameSessionDetailsDto>>(
            gameSession => Ok(GameSessionDetailsDto.FromGameSession(gameSession, clock.GetCurrentInstant())),
            notFound => NotFound()
        );
    }

    [HttpGet]
    [Route("join/{joinCode}")]
    [ProducesResponseType<GameSessionDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<GameSessionDetailsDto>> GetGameSessionByJoinCode([FromRoute] string joinCode)
    {
        OneOf<GameSession, NotFound> sessionResult = await gameSessionService.GetGameSessionByJoinCodeAsync(joinCode, tracking: false);

        return sessionResult.Match<ActionResult<GameSessionDetailsDto>>(
            gameSession => Ok(GameSessionDetailsDto.FromGameSession(gameSession, clock.GetCurrentInstant())),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<GameSessionDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddGameSession([FromBody] GameSessionAddRequest addRequest)
    {
        if (!ValidateRequest<GameSessionAddRequest.Validator, GameSessionAddRequest>(addRequest))
        {
            logger.LogWarning("Rejected game session create request because validation failed for host user {HostUserId}", addRequest.HostUserId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<GameSession, NotFound, DomainError> addResult = await gameSessionService.AddGameSessionAsync(
                new IGameSessionService.GameSessionData(
                    addRequest.HostUserId,
                    addRequest.SessionName,
                    addRequest.JoinCode,
                    addRequest.Status,
                    addRequest.StartTime,
                    addRequest.EndTime,
                    addRequest.PlannedDurationMinutes,
                    addRequest.MrXRevealInterval
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async gameSession =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Created game session {SessionId} for host user {HostUserId}", gameSession.Id, gameSession.HostUserId);

                return CreatedAtAction(nameof(GetGameSessionById), new { sessionId = gameSession.Id },
                    GameSessionDetailsDto.FromGameSession(gameSession, clock.GetCurrentInstant()));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected game session create request because host user {HostUserId} was not found", addRequest.HostUserId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected game session create request with domain error {ErrorCode}: {ErrorMessage}", domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add game session");
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPut]
    [Route("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdateGameSession(
        [FromRoute] int sessionId,
        [FromBody] GameSessionUpdateRequest updateRequest)
    {
        if (!ValidateRequest<GameSessionUpdateRequest.Validator, GameSessionUpdateRequest>(updateRequest))
        {
            logger.LogWarning("Rejected update for game session {SessionId} because validation failed", sessionId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> updateResult = await gameSessionService.UpdateGameSessionAsync(
                sessionId,
                new IGameSessionService.GameSessionData(
                    updateRequest.HostUserId,
                    updateRequest.SessionName,
                    updateRequest.JoinCode,
                    updateRequest.Status,
                    updateRequest.StartTime,
                    updateRequest.EndTime,
                    updateRequest.PlannedDurationMinutes,
                    updateRequest.MrXRevealInterval
                ),
                tracking: true
            );

            return await updateResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Updated game session {SessionId}", sessionId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected update for game session {SessionId} because the session or host user was not found", sessionId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected update for game session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update game session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpDelete]
    [Route("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteGameSession([FromRoute] int sessionId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await gameSessionService.DeleteGameSessionAsync(sessionId, tracking: true);

            return await deleteResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Deleted game session {SessionId}", sessionId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected delete for game session {SessionId} because it was not found", sessionId);

                return NotFound();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete game session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPost]
    [Route("{sessionId:int}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> StartGameSession([FromRoute] int sessionId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> result = await gameSessionService.StartGameSessionAsync(sessionId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();

                OneOf<GameSession, NotFound> sessionResult = await gameSessionService.GetGameSessionByIdAsync(sessionId, tracking: false);
                await sessionResult.Match(
                    gameSession => realtimePublisher.PublishGameSessionStartedAsync(gameSession),
                    _ => ValueTask.CompletedTask);

                logger.LogInformation("Started game session {SessionId}", sessionId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected start for game session {SessionId} because it was not found", sessionId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected start for game session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start game session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPost]
    [Route("{sessionId:int}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> EndGameSession([FromRoute] int sessionId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> result = await gameSessionService.EndGameSessionAsync(sessionId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Ended game session {SessionId}", sessionId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected end for game session {SessionId} because it was not found", sessionId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected end for game session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to end game session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPost]
    [Route("{sessionId:int}/catch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> CatchMrX([FromRoute] int sessionId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> result = await gameSessionService.CatchMrXAsync(sessionId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("MrX was caught in game session {SessionId}", sessionId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected catch for game session {SessionId} because session or MrX team was not found", sessionId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected catch for game session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process catch for game session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }
}

public sealed class GameSessionListResponse
{
    public required List<GameSessionInformationDto> Items { get; init; }
}

public sealed record GameSessionInformationDto(
    int Id,
    string SessionName,
    string JoinCode,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime
)
{
    public static GameSessionInformationDto FromGameSession(GameSession gameSession) =>
        new(
            gameSession.Id,
            gameSession.SessionName,
            gameSession.JoinCode,
            gameSession.Status,
            gameSession.StartTime,
            gameSession.EndTime
        );
}

public sealed record GameSessionDetailsDto(
    int Id,
    int HostUserId,
    string SessionName,
    string JoinCode,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime,
    int PlannedDurationMinutes,
    int MrXRevealInterval,
    Instant ServerNow,
    Instant? NextRevealAt,
    int RevealSecondsRemaining,
    int RevealIntervalSeconds
)
{
    public static GameSessionDetailsDto FromGameSession(GameSession gameSession, Instant serverNow)
    {
        (Instant? nextRevealAt, int revealSecondsRemaining, int revealIntervalSeconds) = GetRevealTiming(gameSession, serverNow);

        return new GameSessionDetailsDto(
            gameSession.Id,
            gameSession.HostUserId,
            gameSession.SessionName,
            gameSession.JoinCode,
            gameSession.Status,
            gameSession.StartTime,
            gameSession.EndTime,
            gameSession.PlannedDurationMinutes,
            gameSession.MrXRevealInterval,
            serverNow,
            nextRevealAt,
            revealSecondsRemaining,
            revealIntervalSeconds
        );
    }

    private static (Instant? NextRevealAt, int RevealSecondsRemaining, int RevealIntervalSeconds) GetRevealTiming(GameSession gameSession, Instant serverNow)
    {
        if (gameSession.Status != SessionStatus.Active || gameSession.StartTime is null || gameSession.MrXRevealInterval <= 0)
        {
            return (null, 0, 0);
        }

        if (!RevealTimingCalculator.TryGetRevealWindow(
                gameSession.StartTime.Value,
                serverNow,
                gameSession.MrXRevealInterval,
                out _,
                out var intervalEnd,
                out var revealIntervalSeconds,
                out var revealSecondsRemaining))
        {
            return (null, 0, 0);
        }

        return (intervalEnd, revealSecondsRemaining, revealIntervalSeconds);
    }
}

public sealed record GameSessionAddRequest(
    int HostUserId,
    string SessionName,
    string JoinCode,
    SessionStatus Status = SessionStatus.Waiting,
    Instant? StartTime = null,
    Instant? EndTime = null,
    int PlannedDurationMinutes = 60,
    int MrXRevealInterval = 5
)
{
    public sealed class Validator : AbstractValidator<GameSessionAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.HostUserId).GreaterThan(0);
            RuleFor(x => x.SessionName).NotEmpty().MaximumLength(120);
            RuleFor(x => x.JoinCode).NotEmpty().Length(6);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.PlannedDurationMinutes).GreaterThan(0);
            RuleFor(x => x.MrXRevealInterval).GreaterThan(0);
        }
    }
}

public sealed record GameSessionUpdateRequest(
    int HostUserId,
    string SessionName,
    string JoinCode,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime,
    int PlannedDurationMinutes,
    int MrXRevealInterval
)
{
    public sealed class Validator : AbstractValidator<GameSessionUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.HostUserId).GreaterThan(0);
            RuleFor(x => x.SessionName).NotEmpty().MaximumLength(120);
            RuleFor(x => x.JoinCode).NotEmpty().Length(6);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.PlannedDurationMinutes).GreaterThan(0);
            RuleFor(x => x.MrXRevealInterval).GreaterThan(0);
        }
    }
}
