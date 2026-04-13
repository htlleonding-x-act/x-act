using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddTeam(
        [FromRoute] int sessionId,
        [FromBody] TeamAddRequest addRequest)
    {
        if (!ValidateRequest<TeamAddRequest.Validator, TeamAddRequest>(addRequest))
        {
            logger.LogWarning("Rejected team create request in session {SessionId} because validation failed", sessionId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Team, NotFound, DomainError> addResult = await teamService.AddTeamAsync(
                new ITeamService.TeamData(
                    sessionId,
                    addRequest.TeamName,
                    addRequest.Role,
                    addRequest.ColorCode,
                    addRequest.IsCaught,
                    addRequest.MaxPlayerCount
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async team =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Created team {TeamId} in session {SessionId}", team.Id, sessionId);
                return CreatedAtAction(nameof(GetTeamById),
                    new { sessionId, teamId = team.Id },
                    TeamDetailsDto.FromTeam(team));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team create request because session {SessionId} was not found", sessionId);
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team create request in session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, domainError.Code, domainError.Message);
                return DomainErrorResult(domainError);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdateTeam(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromBody] TeamUpdateRequest updateRequest)
    {
        if (!ValidateRequest<TeamUpdateRequest.Validator, TeamUpdateRequest>(updateRequest))
        {
            logger.LogWarning("Rejected team update request for team {TeamId} in session {SessionId} because validation failed", teamId, sessionId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> updateResult = await teamService.UpdateTeamAsync(
                sessionId,
                teamId,
                new ITeamService.TeamData(
                    sessionId,
                    updateRequest.TeamName,
                    updateRequest.Role,
                    updateRequest.ColorCode,
                    updateRequest.IsCaught,
                    updateRequest.MaxPlayerCount
                ),
                tracking: true
            );

            return await updateResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Updated team {TeamId} in session {SessionId}", teamId, sessionId);
                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team update request because team {TeamId} or session {SessionId} was not found", teamId, sessionId);
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team update request for team {TeamId} in session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", teamId, sessionId, domainError.Code, domainError.Message);
                return DomainErrorResult(domainError);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> DeleteTeam(
        [FromRoute] int sessionId,
        [FromRoute] int teamId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> deleteResult = await teamService.DeleteTeamAsync(sessionId, teamId, tracking: true);

            return await deleteResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Deleted team {TeamId} from session {SessionId}", teamId, sessionId);
                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team delete request because team {TeamId} or session {SessionId} was not found", teamId, sessionId);
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team delete request for team {TeamId} in session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", teamId, sessionId, domainError.Code, domainError.Message);
                return DomainErrorResult(domainError);
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

public sealed record TeamInformationDto(int Id, int SessionId, string TeamName, TeamRole Role, string ColorCode, int MaxPlayerCount)
{
    public static TeamInformationDto FromTeam(Team team) =>
        new(team.Id, team.SessionId, team.TeamName, team.Role, team.ColorCode, team.MaxPlayerCount);
}

public sealed record TeamDetailsDto(int Id, int SessionId, string TeamName, TeamRole Role, string ColorCode, bool IsCaught, int MaxPlayerCount)
{
    public static TeamDetailsDto FromTeam(Team team) =>
        new(team.Id, team.SessionId, team.TeamName, team.Role, team.ColorCode, team.IsCaught, team.MaxPlayerCount);
}

public sealed record TeamAddRequest(string TeamName, TeamRole Role, string ColorCode, bool IsCaught = false, int MaxPlayerCount = Team.DefaultMaxPlayerCount)
{
    public sealed class Validator : AbstractValidator<TeamAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Role).IsInEnum();
            RuleFor(x => x.ColorCode).Matches("^#[0-9A-Fa-f]{6}$");
            RuleFor(x => x.MaxPlayerCount).GreaterThan(0);
        }
    }
}

public sealed record TeamUpdateRequest(string TeamName, TeamRole Role, string ColorCode, bool IsCaught, int MaxPlayerCount = Team.DefaultMaxPlayerCount)
{
    public sealed class Validator : AbstractValidator<TeamUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Role).IsInEnum();
            RuleFor(x => x.ColorCode).Matches("^#[0-9A-Fa-f]{6}$");
            RuleFor(x => x.MaxPlayerCount).GreaterThan(0);
        }
    }
}
