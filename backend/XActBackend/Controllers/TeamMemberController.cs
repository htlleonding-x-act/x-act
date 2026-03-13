using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

// TODO Review tracking usage

[Route("api/gamesessions/{sessionId:int}/teams/{teamId:int}/members")]
public sealed class TeamMemberController(
    ITransactionProvider transaction,
    ITeamMemberService teamMemberService,
    ILogger<TeamMemberController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<TeamMemberListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<TeamMemberListResponse>> GetMembersByTeamId([FromRoute] int sessionId, [FromRoute] int teamId)
    {
        IReadOnlyCollection<TeamMember> members = await teamMemberService.GetMembersByTeamIdAsync(sessionId, teamId, tracking: false);

        return Ok(new TeamMemberListResponse
        {
            Items = members.Select(TeamMemberInformationDto.FromTeamMember).ToList()
        });
    }

    [HttpGet]
    [Route("{memberId:int}")]
    [ProducesResponseType<TeamMemberDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<TeamMemberDetailsDto>> GetTeamMemberById(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId)
    {
        OneOf<TeamMember, NotFound> memberResult = await teamMemberService.GetTeamMemberByIdAsync(sessionId, teamId, memberId, tracking: false);

        return memberResult.Match<ActionResult<TeamMemberDetailsDto>>(
            member => Ok(TeamMemberDetailsDto.FromTeamMember(member)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<TeamMemberDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddTeamMember(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromBody] TeamMemberAddRequest addRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<TeamMember, Error> addResult = await teamMemberService.AddTeamMemberAsync(
                new ITeamMemberService.TeamMemberData(
                    sessionId,
                    teamId,
                    addRequest.UserId,
                    addRequest.GuestName,
                    addRequest.IsTeamLeader,
                    addRequest.CurrentLatitude,
                    addRequest.CurrentLongitude,
                    addRequest.LastUpdated
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async member =>
            {
                await transaction.CommitAsync();
                return CreatedAtAction(nameof(GetTeamMemberById),
                    new { sessionId, teamId, memberId = member.Id },
                    TeamMemberDetailsDto.FromTeamMember(member));
            }, async error =>
            {
                await transaction.RollbackAsync();
                return BadRequest();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add member to team {TeamId}", teamId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }

    [HttpPut]
    [Route("{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> UpdateTeamMember(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromBody] TeamMemberUpdateRequest updateRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await teamMemberService.UpdateTeamMemberAsync(
                sessionId,
                teamId,
                memberId,
                new ITeamMemberService.TeamMemberData(
                    sessionId,
                    teamId,
                    updateRequest.UserId,
                    updateRequest.GuestName,
                    updateRequest.IsTeamLeader,
                    updateRequest.CurrentLatitude,
                    updateRequest.CurrentLongitude,
                    updateRequest.LastUpdated
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
            logger.LogError(ex, "Failed to update member {MemberId} in team {TeamId}", memberId, teamId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }

    [HttpDelete]
    [Route("{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteTeamMember(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await teamMemberService.DeleteTeamMemberAsync(sessionId, teamId, memberId, tracking: true);

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
            logger.LogError(ex, "Failed to delete member {MemberId} from team {TeamId}", memberId, teamId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }
}

public sealed class TeamMemberListResponse
{
    public required List<TeamMemberInformationDto> Items { get; init; }
}

public sealed record TeamMemberInformationDto(int Id, int SessionId, int TeamId, int? UserId, string? GuestName, bool IsTeamLeader)
{
    public static TeamMemberInformationDto FromTeamMember(TeamMember member) =>
    new(member.Id, member.SessionId, member.TeamId, member.UserId, member.GuestName, member.IsTeamLeader);
}

public sealed record TeamMemberDetailsDto(
    int Id,
    int SessionId,
    int TeamId,
    int? UserId,
    string? GuestName,
    bool IsTeamLeader,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Instant? LastUpdated)
{
    public static TeamMemberDetailsDto FromTeamMember(TeamMember member) =>
        new(member.Id, member.SessionId, member.TeamId, member.UserId, member.GuestName, member.IsTeamLeader,
            member.CurrentLatitude, member.CurrentLongitude, member.LastUpdated);
}

public sealed record TeamMemberAddRequest(
    int? UserId,
    string? GuestName,
    bool IsTeamLeader = false,
    double? CurrentLatitude = null,
    double? CurrentLongitude = null,
    Instant? LastUpdated = null
);

public sealed record TeamMemberUpdateRequest(
    int? UserId,
    string? GuestName,
    bool IsTeamLeader,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Instant? LastUpdated
);
