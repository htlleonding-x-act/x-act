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

[Route("api/gamesessions/{sessionId:int}/report")]
public sealed class ReportController(
    ITransactionProvider transaction,
    IReportService reportService,
    IOffenseService offenseService,
    IGameSessionRealtimePublisher realtimePublisher,
    IClock clock,
    ILogger<ReportController> logger) : BaseController
{
    [HttpGet]
    [Route("votes/open")]
    [ProducesResponseType<KickVoteOpenResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<KickVoteOpenResponse>> GetOpenVote([FromRoute] int sessionId)
    {
        IReportService.KickVoteView? view = await reportService.GetOpenVoteAsync(sessionId);

        return Ok(new KickVoteOpenResponse
        {
            Vote = view is null ? null : KickVotePayload.FromView(view)
        });
    }

    [HttpGet]
    [Route("offenses")]
    [ProducesResponseType<OffenseListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<OffenseListResponse>> GetActiveOffenses([FromRoute] int sessionId)
    {
        IReadOnlyCollection<Offense> offenses = await offenseService.GetActiveOffensesBySessionAsync(sessionId, tracking: false);

        return Ok(new OffenseListResponse
        {
            Items = offenses.Select(MemberOffensePayload.FromOffense).ToList()
        });
    }

    [HttpPost]
    [Route("votes")]
    [ProducesResponseType<KickVotePayload>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> StartVote(
        [FromRoute] int sessionId,
        [FromBody] StartKickVoteRequest request)
    {
        if (!ValidateRequest<StartKickVoteRequest.Validator, StartKickVoteRequest>(request))
        {
            return BadRequest();
        }

        return await RunVoteActionAsync(
            sessionId,
            () => reportService.StartKickVoteAsync(sessionId, request.InitiatorMemberId, request.TargetMemberId, request.Reason),
            started: true);
    }

    [HttpPost]
    [Route("votes/{voteId:int}/ballots")]
    [ProducesResponseType<KickVotePayload>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> CastBallot(
        [FromRoute] int sessionId,
        [FromRoute] int voteId,
        [FromBody] CastBallotRequest request)
    {
        if (!ValidateRequest<CastBallotRequest.Validator, CastBallotRequest>(request))
        {
            return BadRequest();
        }

        return await RunVoteActionAsync(
            sessionId,
            () => reportService.CastBallotAsync(sessionId, voteId, request.VoterMemberId, request.Approve),
            started: false);
    }

    [HttpPost]
    [Route("votes/{voteId:int}/cancel")]
    [ProducesResponseType<KickVotePayload>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> CancelVote(
        [FromRoute] int sessionId,
        [FromRoute] int voteId,
        [FromBody] CancelKickVoteRequest request)
    {
        if (!ValidateRequest<CancelKickVoteRequest.Validator, CancelKickVoteRequest>(request))
        {
            return BadRequest();
        }

        return await RunVoteActionAsync(
            sessionId,
            () => reportService.CancelKickVoteAsync(sessionId, voteId, request.ActingMemberId),
            started: false);
    }

    [HttpPost]
    [Route("kick")]
    [ProducesResponseType<MemberKickedPayload>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> HostKick(
        [FromRoute] int sessionId,
        [FromBody] HostKickRequest request)
    {
        if (!ValidateRequest<HostKickRequest.Validator, HostKickRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<IReportService.HostKickResult, NotFound, DomainError> result =
                await reportService.HostKickMemberAsync(sessionId, request.ActingMemberId, request.TargetMemberId);

            return await result.Match<ValueTask<IActionResult>>(async hostKick =>
            {
                await transaction.CommitAsync();

                MemberKickedPayload payload = await PublishKickedAsync(
                    hostKick.KickedMember,
                    hostKick.KickedMemberName,
                    KickReasons.Host,
                    request.Reason);

                if (hostKick.ResolvedVote is not null)
                {
                    await realtimePublisher.PublishKickVoteResolvedAsync(KickVotePayload.FromView(hostKick.ResolvedVote));
                }

                logger.LogInformation("Host kick succeeded for member {TargetId} in session {SessionId}", request.TargetMemberId, sessionId);
                return Ok(payload);
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected host kick in session {SessionId} with domain error {ErrorCode}", sessionId, domainError.Code);
                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed host kick in session {SessionId}", sessionId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }

    private async ValueTask<IActionResult> RunVoteActionAsync(
        int sessionId,
        Func<ValueTask<OneOf<IReportService.KickVoteActionResult, NotFound, DomainError>>> action,
        bool started)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result = await action();

            return await result.Match<ValueTask<IActionResult>>(async actionResult =>
            {
                await transaction.CommitAsync();

                var payload = KickVotePayload.FromView(actionResult.Vote);

                if (actionResult.Resolved)
                {
                    await realtimePublisher.PublishKickVoteResolvedAsync(payload);

                    if (actionResult.KickedMember is not null)
                    {
                        await PublishKickedAsync(
                            actionResult.KickedMember,
                            actionResult.KickedMemberName,
                            KickReasons.Vote,
                            actionResult.Vote.Reason);
                    }
                }
                else if (started)
                {
                    await realtimePublisher.PublishKickVoteStartedAsync(payload);
                }
                else
                {
                    await realtimePublisher.PublishKickVoteUpdatedAsync(payload);
                }

                return started && !actionResult.Resolved
                    ? StatusCode(StatusCodes.Status201Created, payload)
                    : Ok(payload);
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected report vote action in session {SessionId} with domain error {ErrorCode}", sessionId, domainError.Code);
                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed report vote action in session {SessionId}", sessionId);
            await transaction.RollbackAsync();
            return Problem();
        }
    }

    private async ValueTask<MemberKickedPayload> PublishKickedAsync(TeamMember member, string? memberName, string kickType, string? reason)
    {
        Instant now = clock.GetCurrentInstant();

        var payload = new MemberKickedPayload(
            member.SessionId,
            member.TeamId,
            member.Id,
            member.UserId,
            member.GuestName,
            memberName ?? "Unknown",
            kickType,
            reason,
            now);

        await realtimePublisher.PublishMemberKickedAsync(payload);

        // Also raise the standard "left" event so existing rosters and lobby views update.
        await realtimePublisher.PublishTeamMemberLeftAsync(
            member.SessionId,
            member.TeamId,
            member.Id,
            member.UserId,
            member.GuestName,
            now);

        return payload;
    }
}

public sealed class KickVoteOpenResponse
{
    public required KickVotePayload? Vote { get; init; }
}

public sealed class OffenseListResponse
{
    public required List<MemberOffensePayload> Items { get; init; }
}

public sealed record StartKickVoteRequest(
    int InitiatorMemberId,
    int TargetMemberId,
    string? Reason
)
{
    public sealed class Validator : AbstractValidator<StartKickVoteRequest>
    {
        public Validator()
        {
            RuleFor(x => x.InitiatorMemberId).GreaterThan(0);
            RuleFor(x => x.TargetMemberId).GreaterThan(0);
            RuleFor(x => x.Reason).MaximumLength(KickVote.MaxReasonLength).When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}

public sealed record CastBallotRequest(
    int VoterMemberId,
    bool Approve
)
{
    public sealed class Validator : AbstractValidator<CastBallotRequest>
    {
        public Validator()
        {
            RuleFor(x => x.VoterMemberId).GreaterThan(0);
        }
    }
}

public sealed record CancelKickVoteRequest(
    int ActingMemberId
)
{
    public sealed class Validator : AbstractValidator<CancelKickVoteRequest>
    {
        public Validator()
        {
            RuleFor(x => x.ActingMemberId).GreaterThan(0);
        }
    }
}

public sealed record HostKickRequest(
    int ActingMemberId,
    int TargetMemberId,
    string? Reason
)
{
    public sealed class Validator : AbstractValidator<HostKickRequest>
    {
        public Validator()
        {
            RuleFor(x => x.ActingMemberId).GreaterThan(0);
            RuleFor(x => x.TargetMemberId).GreaterThan(0);
            RuleFor(x => x.Reason).MaximumLength(KickVote.MaxReasonLength).When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}
