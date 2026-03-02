using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

// TODO Review tracking usage

[Route("api/gamesessions")]
public sealed class GameSessionController(
    ITransactionProvider transaction,
    IGameSessionService gameSessionService,
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
            gameSession => Ok(GameSessionDetailsDto.FromGameSession(gameSession)),
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
            gameSession => Ok(GameSessionDetailsDto.FromGameSession(gameSession)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<GameSessionDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddGameSession([FromBody] GameSessionAddRequest addRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<GameSession, Error> addResult = await gameSessionService.AddGameSessionAsync(
                new IGameSessionService.GameSessionData(
                    addRequest.HostUserId,
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

                return CreatedAtAction(nameof(GetGameSessionById), new { sessionId = gameSession.Id },
                    GameSessionDetailsDto.FromGameSession(gameSession));
            }, async error =>
            {
                await transaction.RollbackAsync();

                return BadRequest();
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
    public async ValueTask<IActionResult> UpdateGameSession(
        [FromRoute] int sessionId,
        [FromBody] GameSessionUpdateRequest updateRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await gameSessionService.UpdateGameSessionAsync(
                sessionId,
                new IGameSessionService.GameSessionData(
                    updateRequest.HostUserId,
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

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();

                return NotFound();
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

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();

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
}

public sealed class GameSessionListResponse
{
    public required List<GameSessionInformationDto> Items { get; init; }
}

public sealed record GameSessionInformationDto(
    int Id,
    string JoinCode,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime
)
{
    public static GameSessionInformationDto FromGameSession(GameSession gameSession) =>
        new(
            gameSession.Id,
            gameSession.JoinCode,
            gameSession.Status,
            gameSession.StartTime,
            gameSession.EndTime
        );
}

public sealed record GameSessionDetailsDto(
    int Id,
    int HostUserId,
    string JoinCode,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime,
    int PlannedDurationMinutes,
    int MrXRevealInterval
)
{
    public static GameSessionDetailsDto FromGameSession(GameSession gameSession) =>
        new(
            gameSession.Id,
            gameSession.HostUserId,
            gameSession.JoinCode,
            gameSession.Status,
            gameSession.StartTime,
            gameSession.EndTime,
            gameSession.PlannedDurationMinutes,
            gameSession.MrXRevealInterval
        );
}

public sealed record GameSessionAddRequest(
    int HostUserId,
    string JoinCode,
    SessionStatus Status = SessionStatus.Waiting,
    Instant? StartTime = null,
    Instant? EndTime = null,
    int PlannedDurationMinutes = 60,
    int MrXRevealInterval = 5
);

public sealed record GameSessionUpdateRequest(
    int HostUserId,
    string JoinCode,
    SessionStatus Status,
    Instant? StartTime,
    Instant? EndTime,
    int PlannedDurationMinutes,
    int MrXRevealInterval
);