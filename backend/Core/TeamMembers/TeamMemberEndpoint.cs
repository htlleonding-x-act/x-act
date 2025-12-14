using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.TeamMembers;

public static class TeamMemberEndpoint
{
    private const string ApiBasePath = "/api/teammembers";

    public static void MapTeamMemberEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] ITeamMemberService service) =>
            {
                IEnumerable<TeamMember> teamMembers = await service.GetAllTeamMembersAsync();

                return Results.Ok(new TeamMemberListResponse
                {
                    Items = [.. teamMembers.Select(TeamMemberInformationDto.FromTeamMember)]
                });
            })
            .Produces<TeamMemberListResponse>(StatusCodes.Status200OK);

        group.MapGet("{memberId:guid}", async (
            [FromRoute] Guid memberId,
            [FromServices] ITeamMemberService service) =>
            {
                OneOf<TeamMember, NotFound> teamMemberResult = await service.GetTeamMemberByIdAsync(memberId);

                return teamMemberResult.Match(
                    teamMember => Results.Ok(TeamMemberDetailsDto.FromTeamMember(teamMember)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<TeamMemberDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] TeamMemberAddRequest newTeamMember,
            [FromServices] ITeamMemberService service) =>
            {
                OneOf<TeamMember, Error> addResult = await service
                .AddTeamMemberAsync(
                    new ITeamMemberService.TeamMemberData(
                        newTeamMember.TeamId,
                        newTeamMember.UserId,
                        newTeamMember.IsTeamLeader,
                        newTeamMember.CurrentLatitude,
                        newTeamMember.CurrentLongitude,
                        newTeamMember.LastUpdated
                    )
                );

                return addResult.Match(
                    teamMember => Results.Created($"{ApiBasePath}/{teamMember.MemberId}", TeamMemberDetailsDto.FromTeamMember(teamMember)),
                    error => Results.BadRequest()
                );
            })
            .Produces<TeamMemberDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{memberId:guid}", async (
            [FromRoute] Guid memberId,
            [FromBody] TeamMemberUpdateRequest teamMemberUpdate,
            [FromServices] ITeamMemberService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdateTeamMemberAsync(
                    memberId,
                    new ITeamMemberService.TeamMemberData(
                        teamMemberUpdate.TeamId,
                        teamMemberUpdate.UserId,
                        teamMemberUpdate.IsTeamLeader,
                        teamMemberUpdate.CurrentLatitude,
                        teamMemberUpdate.CurrentLongitude,
                        teamMemberUpdate.LastUpdated
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{memberId:guid}", async (
            [FromRoute] Guid memberId,
            [FromServices] ITeamMemberService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeleteTeamMemberAsync(memberId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record TeamMemberListResponse
    {
        public required IEnumerable<TeamMemberInformationDto> Items { get; init; }
    }

    private sealed record TeamMemberInformationDto(
        Guid MemberId,
        Guid TeamId,
        Guid UserId,
        bool IsTeamLeader
    )
    {
        public static TeamMemberInformationDto FromTeamMember(TeamMember teamMember) =>
            new(
                teamMember.MemberId,
                teamMember.TeamId,
                teamMember.UserId,
                teamMember.IsTeamLeader
            );
    }

    private sealed record TeamMemberDetailsDto(
        Guid MemberId,
        Guid TeamId,
        Guid UserId,
        bool IsTeamLeader,
        double? CurrentLatitude,
        double? CurrentLongitude,
        Instant? LastUpdated
    )
    {
        public static TeamMemberDetailsDto FromTeamMember(TeamMember teamMember) =>
            new(
                teamMember.MemberId,
                teamMember.TeamId,
                teamMember.UserId,
                teamMember.IsTeamLeader,
                teamMember.CurrentLatitude,
                teamMember.CurrentLongitude,
                teamMember.LastUpdated
            );
    }

    private sealed record TeamMemberAddRequest(
        Guid TeamId,
        Guid UserId,
        bool IsTeamLeader = false,
        double? CurrentLatitude = null,
        double? CurrentLongitude = null,
        Instant? LastUpdated = null
    );

    private sealed record TeamMemberUpdateRequest(
        Guid TeamId,
        Guid UserId,
        bool IsTeamLeader,
        double? CurrentLatitude,
        double? CurrentLongitude,
        Instant? LastUpdated
    );
}
