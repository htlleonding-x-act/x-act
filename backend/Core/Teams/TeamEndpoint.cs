using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.Teams;

public static class TeamEndpoint
{
    private const string ApiBasePath = "/api/teams";

    public static void MapTeamEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] ITeamService service) =>
            {
                IEnumerable<Team> teams = await service.GetAllTeamsAsync();

                return Results.Ok(new TeamListResponse
                {
                    Items = [.. teams.Select(TeamInformationDto.FromTeam)]
                });
            })
            .Produces<TeamListResponse>(StatusCodes.Status200OK);

        group.MapGet("{teamId:int}", async (
            [FromRoute] int teamId,
            [FromServices] ITeamService service) =>
            {
                OneOf<Team, NotFound> teamResult = await service.GetTeamByIdAsync(teamId);

                return teamResult.Match(
                    team => Results.Ok(TeamDetailsDto.FromTeam(team)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<TeamDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] TeamAddRequest newTeam,
            [FromServices] ITeamService service) =>
            {
                OneOf<Team, Error> addResult = await service
                .AddTeamAsync(
                    new ITeamService.TeamData(
                        newTeam.SessionId,
                        newTeam.TeamName,
                        newTeam.Role,
                        newTeam.ColorCode,
                        newTeam.IsCaught
                    )
                );

                return addResult.Match(
                    team => Results.Created($"{ApiBasePath}/{team.TeamId}", TeamDetailsDto.FromTeam(team)),
                    error => Results.BadRequest()
                );
            })
            .Produces<TeamDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{teamId:int}", async (
            [FromRoute] int teamId,
            [FromBody] TeamUpdateRequest teamUpdate,
            [FromServices] ITeamService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdateTeamAsync(
                    teamId,
                    new ITeamService.TeamData(
                        teamUpdate.SessionId,
                        teamUpdate.TeamName,
                        teamUpdate.Role,
                        teamUpdate.ColorCode,
                        teamUpdate.IsCaught
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{teamId:int}", async (
            [FromRoute] int teamId,
            [FromServices] ITeamService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeleteTeamAsync(teamId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record TeamListResponse
    {
        public required IEnumerable<TeamInformationDto> Items { get; init; }
    }

    private sealed record TeamInformationDto(
        int TeamId,
        string TeamName,
        TeamRole Role,
        string ColorCode
    )
    {
        public static TeamInformationDto FromTeam(Team team) =>
            new(
                team.TeamId,
                team.TeamName,
                team.Role,
                team.ColorCode
            );
    }

    private sealed record TeamDetailsDto(
        int TeamId,
        int SessionId,
        string TeamName,
        TeamRole Role,
        string ColorCode,
        bool IsCaught
    )
    {
        public static TeamDetailsDto FromTeam(Team team) =>
            new(
                team.TeamId,
                team.SessionId,
                team.TeamName,
                team.Role,
                team.ColorCode,
                team.IsCaught
            );
    }

    private sealed record TeamAddRequest(
        int SessionId,
        string TeamName,
        TeamRole Role,
        string ColorCode,
        bool IsCaught = false
    );

    private sealed record TeamUpdateRequest(
        int SessionId,
        string TeamName,
        TeamRole Role,
        string ColorCode,
        bool IsCaught
    );
}
