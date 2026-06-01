using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions/{sessionId:int}/teams/{teamId:int}/members")]
public sealed class TeamMemberController(
    ITransactionProvider transaction,
    ITeamMemberService teamMemberService,
    IGameSessionRealtimePublisher realtimePublisher,
    IClock clock,
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddTeamMember(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromBody] TeamMemberAddRequest addRequest)
    {
        if (!ValidateRequest<TeamMemberAddRequest.Validator, TeamMemberAddRequest>(addRequest))
        {
            logger.LogWarning("Rejected team member create request in session {SessionId}, team {TeamId} because validation failed", sessionId, teamId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<TeamMember, NotFound, DomainError> addResult = await teamMemberService.AddTeamMemberAsync(
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
                await realtimePublisher.PublishTeamMemberJoinedAsync(member);
                logger.LogInformation("Created team member {MemberId} in session {SessionId}, team {TeamId}", member.Id, sessionId, teamId);
                return CreatedAtAction(nameof(GetTeamMemberById),
                    new { sessionId, teamId, memberId = member.Id },
                    TeamMemberDetailsDto.FromTeamMember(member));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team member create request because a referenced resource was not found in session {SessionId}, team {TeamId}", sessionId, teamId);
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team member create request in session {SessionId}, team {TeamId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, teamId, domainError.Code, domainError.Message);
                return DomainErrorResult(domainError);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdateTeamMember(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromBody] TeamMemberUpdateRequest updateRequest)
    {
        if (!ValidateRequest<TeamMemberUpdateRequest.Validator, TeamMemberUpdateRequest>(updateRequest))
        {
            logger.LogWarning("Rejected team member update request for member {MemberId} in session {SessionId}, team {TeamId} because validation failed", memberId, sessionId, teamId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> updateResult = await teamMemberService.UpdateTeamMemberAsync(
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

                var updatedMemberResult = await teamMemberService.GetTeamMemberByIdAsync(sessionId, teamId, memberId, tracking: false);
                await updatedMemberResult.Match(
                    member => realtimePublisher.PublishTeamMemberUpdatedAsync(member),
                    _ => ValueTask.CompletedTask);

                logger.LogInformation("Updated team member {MemberId} in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team member update request because a referenced resource was not found for member {MemberId} in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team member update request for member {MemberId} in session {SessionId}, team {TeamId} with domain error {ErrorCode}: {ErrorMessage}", memberId, sessionId, teamId, domainError.Code, domainError.Message);
                return DomainErrorResult(domainError);
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
        OneOf<TeamMember, NotFound> existingMemberResult =
            await teamMemberService.GetTeamMemberByIdAsync(sessionId, teamId, memberId, tracking: false);

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await teamMemberService.DeleteTeamMemberAsync(sessionId, teamId, memberId, tracking: true);

            return await deleteResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();

                await existingMemberResult.Match(
                    member => realtimePublisher.PublishTeamMemberLeftAsync(
                        sessionId,
                        teamId,
                        member.Id,
                        member.UserId,
                        member.GuestName,
                        clock.GetCurrentInstant()),
                    _ => ValueTask.CompletedTask);

                logger.LogInformation("Deleted team member {MemberId} from session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected team member delete request because member {MemberId} was not found in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
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

public sealed record TeamMemberInformationDto(int Id, int SessionId, int TeamId, string? UserId, string? GuestName, bool IsTeamLeader)
{
    public static TeamMemberInformationDto FromTeamMember(TeamMember member) =>
    new(member.Id, member.SessionId, member.TeamId, member.UserId, member.GuestName, member.IsTeamLeader);
}

public sealed record TeamMemberDetailsDto(
    int Id,
    int SessionId,
    int TeamId,
    string? UserId,
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
    string? UserId,
    string? GuestName,
    bool IsTeamLeader = false,
    double? CurrentLatitude = null,
    double? CurrentLongitude = null,
    Instant? LastUpdated = null
)
{
    public sealed class Validator : AbstractValidator<TeamMemberAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.UserId));
            RuleFor(x => x.GuestName).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.GuestName));
            RuleFor(x => x).Must(x => (!string.IsNullOrWhiteSpace(x.UserId) && string.IsNullOrWhiteSpace(x.GuestName))
                                  || (string.IsNullOrWhiteSpace(x.UserId) && !string.IsNullOrWhiteSpace(x.GuestName)))
                          .WithMessage("Either UserId or GuestName must be set, but not both.");
            RuleFor(x => x.CurrentLatitude).InclusiveBetween(-90, 90).When(x => x.CurrentLatitude.HasValue);
            RuleFor(x => x.CurrentLongitude).InclusiveBetween(-180, 180).When(x => x.CurrentLongitude.HasValue);
        }
    }
}

public sealed record TeamMemberUpdateRequest(
    string? UserId,
    string? GuestName,
    bool IsTeamLeader,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Instant? LastUpdated
)
{
    public sealed class Validator : AbstractValidator<TeamMemberUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.UserId));
            RuleFor(x => x.GuestName).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.GuestName));
            RuleFor(x => x).Must(x => (!string.IsNullOrWhiteSpace(x.UserId) && string.IsNullOrWhiteSpace(x.GuestName))
                                  || (string.IsNullOrWhiteSpace(x.UserId) && !string.IsNullOrWhiteSpace(x.GuestName)))
                          .WithMessage("Either UserId or GuestName must be set, but not both.");
            RuleFor(x => x.CurrentLatitude).InclusiveBetween(-90, 90).When(x => x.CurrentLatitude.HasValue);
            RuleFor(x => x.CurrentLongitude).InclusiveBetween(-180, 180).When(x => x.CurrentLongitude.HasValue);
        }
    }
}
