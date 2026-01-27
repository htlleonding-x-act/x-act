using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.GameSessions;

public static class GameSessionEndpoint
{
    private const string ApiBasePath = "/api/gamesessions";

    public static void MapGameSessionEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] IGameSessionService service) =>
            {
                IEnumerable<GameSession> gameSessions = await service.GetAllGameSessionsAsync();

                return Results.Ok(new GameSessionListResponse
                {
                    Items = [.. gameSessions.Select(GameSessionInformationDto.FromGameSession)]
                });
            })
            .Produces<GameSessionListResponse>(StatusCodes.Status200OK);

        group.MapGet("{sessionId:int}", async (
            [FromRoute] int sessionId,
            [FromServices] IGameSessionService service) =>
            {
                OneOf<GameSession, NotFound> gameSessionResult = await service.GetGameSessionByIdAsync(sessionId);

                return gameSessionResult.Match(
                    gameSession => Results.Ok(GameSessionDetailsDto.FromGameSession(gameSession)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<GameSessionDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] GameSessionAddRequest newGameSession,
            [FromServices] IGameSessionService service) =>
            {
                OneOf<GameSession, Error> addResult = await service
                .AddGameSessionAsync(
                    new IGameSessionService.GameSessionData(
                        newGameSession.HostUserId,
                        newGameSession.JoinCode,
                        newGameSession.Status,
                        newGameSession.StartTime,
                        newGameSession.EndTime,
                        newGameSession.PlannedDurationMinutes,
                        newGameSession.MrXRevealInterval
                    )
                );

                return addResult.Match(
                    gameSession => Results.Created($"{ApiBasePath}/{gameSession.SessionId}", GameSessionDetailsDto.FromGameSession(gameSession)),
                    error => Results.BadRequest()
                );
            })
            .Produces<GameSessionDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{sessionId:int}", async (
            [FromRoute] int sessionId,
            [FromBody] GameSessionUpdateRequest gameSessionUpdate,
            [FromServices] IGameSessionService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdateGameSessionAsync(
                    sessionId,
                    new IGameSessionService.GameSessionData(
                        gameSessionUpdate.HostUserId,
                        gameSessionUpdate.JoinCode,
                        gameSessionUpdate.Status,
                        gameSessionUpdate.StartTime,
                        gameSessionUpdate.EndTime,
                        gameSessionUpdate.PlannedDurationMinutes,
                        gameSessionUpdate.MrXRevealInterval
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{sessionId:int}", async (
            [FromRoute] int sessionId,
            [FromServices] IGameSessionService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeleteGameSessionAsync(sessionId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record GameSessionListResponse
    {
        public required IEnumerable<GameSessionInformationDto> Items { get; init; }
    }

    private sealed record GameSessionInformationDto(
        int SessionId,
        string JoinCode,
        SessionStatus Status,
        Instant? StartTime,
        Instant? EndTime
    )
    {
        public static GameSessionInformationDto FromGameSession(GameSession gameSession) =>
            new(
                gameSession.SessionId,
                gameSession.JoinCode,
                gameSession.Status,
                gameSession.StartTime,
                gameSession.EndTime
            );
    }

    private sealed record GameSessionDetailsDto(
        int SessionId,
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
                gameSession.SessionId,
                gameSession.HostUserId,
                gameSession.JoinCode,
                gameSession.Status,
                gameSession.StartTime,
                gameSession.EndTime,
                gameSession.PlannedDurationMinutes,
                gameSession.MrXRevealInterval
            );
    }

    private sealed record GameSessionAddRequest(
        int HostUserId,
        string JoinCode,
        SessionStatus Status = SessionStatus.WAITING,
        Instant? StartTime = null,
        Instant? EndTime = null,
        int PlannedDurationMinutes = 60,
        int MrXRevealInterval = 5
    );

    private sealed record GameSessionUpdateRequest(
        int HostUserId,
        string JoinCode,
        SessionStatus Status,
        Instant? StartTime,
        Instant? EndTime,
        int PlannedDurationMinutes,
        int MrXRevealInterval
    );
}
