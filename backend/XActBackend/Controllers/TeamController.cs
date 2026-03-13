using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

// TODO Review tracking usage

[Route("api/gamesessions/{sessionId:int}/teams")]
public sealed class TeamController(
    ITransactionProvider transaction,
    ITeamService teamService,
    ILogger<TeamController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<TeamListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<TeamListResponse>> GetTeamsBySessionId([FromRoute] int sessionId)
    {
        IReadOnlyCollection<Team> teams = await teamService.GetTeamsBySessionIdAsync(sessionId, tracking: false);

        return Ok(new TeamListResponse
        {
            Items = teams.Select(TeamInformationDto.FromTeam).ToList()
        });
    }

    [HttpGet]
    [Route("{teamId:int}")]
    [ProducesResponseType<TeamDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<TeamDetailsDto>> GetTeamById(
        [FromRoute] int sessionId,
        [FromRoute] int teamId)
    {
        OneOf<Team, NotFound> teamResult = await teamService.GetTeamByIdAsync(sessionId, teamId, tracking: false);

        return teamResult.Match<ActionResult<TeamDetailsDto>>(
            team => Ok(TeamDetailsDto.FromTeam(team)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<TeamDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddTeam(
        [FromRoute] int sessionId,
        [FromBody] TeamAddRequest addRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Team, Error> addResult = await teamService.AddTeamAsync(
                new ITeamService.TeamData(
                    sessionId,
                    addRequest.TeamName,
                    addRequest.Role,
                    addRequest.ColorCode,
                    addRequest.IsCaught
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async team =>
            {
                await transaction.CommitAsync();
                return CreatedAtAction(nameof(GetTeamById),
                    new { sessionId, teamId = team.Id },
                    TeamDetailsDto.FromTeam(team));
            }, async error =>
            {
                await transaction.RollbackAsync();
                return BadRequest();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add team for session {SessionId}", sessionId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }

    [HttpPut]
    [Route("{teamId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> UpdateTeam(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromBody] TeamUpdateRequest updateRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await teamService.UpdateTeamAsync(
                sessionId,
                teamId,
                new ITeamService.TeamData(
                    sessionId,
                    updateRequest.TeamName,
                    updateRequest.Role,
                    updateRequest.ColorCode,
                    updateRequest.IsCaught
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
            logger.LogError(ex, "Failed to update team {TeamId} for session {SessionId}", teamId, sessionId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }

    [HttpDelete]
    [Route("{teamId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteTeam(
        [FromRoute] int sessionId,
        [FromRoute] int teamId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await teamService.DeleteTeamAsync(sessionId, teamId, tracking: true);

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
            logger.LogError(ex, "Failed to delete team {TeamId} for session {SessionId}", teamId, sessionId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }
}

public sealed class TeamListResponse
{
    public required List<TeamInformationDto> Items { get; init; }
}

public sealed record TeamInformationDto(int Id, int SessionId, string TeamName, TeamRole Role, string ColorCode)
{
    public static TeamInformationDto FromTeam(Team team) =>
        new(team.Id, team.SessionId, team.TeamName, team.Role, team.ColorCode);
}

public sealed record TeamDetailsDto(int Id, int SessionId, string TeamName, TeamRole Role, string ColorCode, bool IsCaught)
{
    public static TeamDetailsDto FromTeam(Team team) =>
        new(team.Id, team.SessionId, team.TeamName, team.Role, team.ColorCode, team.IsCaught);
}

public sealed record TeamAddRequest(string TeamName, TeamRole Role, string ColorCode, bool IsCaught = false);

public sealed record TeamUpdateRequest(string TeamName, TeamRole Role, string ColorCode, bool IsCaught);
