using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Drives the report/voting system: members can start a vote to kick a misbehaving player,
///     cast ballots, and cancel votes, while the host can instantly kick a player with sudo powers.
///     The host can never be the target of a vote or a kick.
/// </summary>
public interface IReportService
{
    /// <summary>
    ///     Get the session's single open kick vote, if one is currently running and not yet expired.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <returns>The open vote view, or <c>null</c> when there is none</returns>
    public ValueTask<KickVoteView?> GetOpenVoteAsync(int sessionId);

    /// <summary>
    ///     Start a kick vote against a target member. The initiator automatically approves.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="initiatorMemberId">The member starting the vote</param>
    /// <param name="targetMemberId">The member the vote wants to kick</param>
    /// <param name="reason">Optional reason for the kick</param>
    /// <returns>The vote result, not found or a domain error if the vote is not allowed</returns>
    public ValueTask<OneOf<KickVoteActionResult, NotFound, DomainError>> StartKickVoteAsync(int sessionId, int initiatorMemberId, int targetMemberId, string? reason);

    /// <summary>
    ///     Cast a ballot in an open kick vote. Resolves the vote immediately when the outcome is decided.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="voteId">The id of the kick vote</param>
    /// <param name="voterMemberId">The member casting the ballot</param>
    /// <param name="approve"><c>true</c> approves the kick, <c>false</c> votes to keep the target</param>
    /// <returns>The vote result, not found or a domain error if the ballot is not allowed</returns>
    public ValueTask<OneOf<KickVoteActionResult, NotFound, DomainError>> CastBallotAsync(int sessionId, int voteId, int voterMemberId, bool approve);

    /// <summary>
    ///     Cancel an open kick vote. Only the initiator or the host may cancel.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="voteId">The id of the kick vote</param>
    /// <param name="actingMemberId">The member requesting the cancellation</param>
    /// <returns>The vote result, not found or a domain error if the cancellation is not allowed</returns>
    public ValueTask<OneOf<KickVoteActionResult, NotFound, DomainError>> CancelKickVoteAsync(int sessionId, int voteId, int actingMemberId);

    /// <summary>
    ///     Instantly kick a member using host sudo powers, bypassing any vote. The acting member must
    ///     be the host and the target must not be the host.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="actingMemberId">The member invoking host powers</param>
    /// <param name="targetMemberId">The member to kick</param>
    /// <returns>The host kick result, not found or a domain error if the kick is not allowed</returns>
    public ValueTask<OneOf<HostKickResult, NotFound, DomainError>> HostKickMemberAsync(int sessionId, int actingMemberId, int targetMemberId);

    /// <summary>
    ///     A snapshot of a kick vote and its current tally, ready to be shown or broadcast.
    /// </summary>
    public sealed record KickVoteView(
        int VoteId,
        int SessionId,
        int? TargetMemberId,
        string TargetName,
        int? InitiatorMemberId,
        string InitiatorName,
        string? Reason,
        KickVoteStatus Status,
        int ApproveCount,
        int RejectCount,
        int EligibleVoterCount,
        Instant CreatedAt,
        Instant ExpiresAt,
        Instant? ResolvedAt
    );

    /// <summary>
    ///     The outcome of a kick-vote action (start/cast/cancel).
    /// </summary>
    /// <param name="Vote">The current view of the vote</param>
    /// <param name="Resolved">Whether the vote is no longer open after this action</param>
    /// <param name="KickedMember">The member removed when the vote passed, otherwise <c>null</c></param>
    /// <param name="KickedMemberName">The display name of the kicked member, if any</param>
    public sealed record KickVoteActionResult(
        KickVoteView Vote,
        bool Resolved,
        TeamMember? KickedMember,
        string? KickedMemberName
    );

    /// <summary>
    ///     The outcome of a host sudo kick.
    /// </summary>
    /// <param name="KickedMember">The member that was removed</param>
    /// <param name="KickedMemberName">The display name of the kicked member</param>
    /// <param name="ResolvedVote">An open vote against the same member that was cancelled, if any</param>
    public sealed record HostKickResult(
        TeamMember KickedMember,
        string KickedMemberName,
        KickVoteView? ResolvedVote
    );
}

internal sealed class ReportService(IUnitOfWork uow, IClock clock, ILogger<ReportService> logger) : IReportService
{
    private const string UnknownName = "Unknown";

    public async ValueTask<IReportService.KickVoteView?> GetOpenVoteAsync(int sessionId)
    {
        var vote = await uow.KickVoteRepository.GetOpenVoteBySessionAsync(sessionId, tracking: false);
        if (vote is null)
        {
            return null;
        }

        // A vote past its window is treated as no longer open; it is lazily expired on the next start.
        if (clock.GetCurrentInstant() > vote.ExpiresAt)
        {
            return null;
        }

        return await BuildViewAsync(vote);
    }

    public async ValueTask<OneOf<IReportService.KickVoteActionResult, NotFound, DomainError>> StartKickVoteAsync(int sessionId, int initiatorMemberId, int targetMemberId, string? reason)
    {
        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            return new NotFound();
        }

        if (session.Status != SessionStatus.Active)
        {
            logger.LogWarning("Rejected kick vote start because session {SessionId} is in status {Status}", sessionId, session.Status);
            return DomainError.SessionNotActive(sessionId, session.Status);
        }

        var initiator = await uow.TeamMemberRepository.GetMemberByIdAsync(initiatorMemberId, tracking: false);
        if (initiator is null || initiator.SessionId != sessionId)
        {
            return new NotFound();
        }

        var target = await uow.TeamMemberRepository.GetMemberByIdAsync(targetMemberId, tracking: false);
        if (target is null || target.SessionId != sessionId)
        {
            return new NotFound();
        }

        if (targetMemberId == initiatorMemberId)
        {
            return DomainError.ReportTargetIsSelf(initiatorMemberId);
        }

        if (IsHost(target, session))
        {
            return DomainError.ReportTargetIsHost(targetMemberId);
        }

        Instant now = clock.GetCurrentInstant();

        var existing = await uow.KickVoteRepository.GetOpenVoteBySessionAsync(sessionId, tracking: true);
        if (existing is not null)
        {
            if (now <= existing.ExpiresAt)
            {
                return DomainError.ReportVoteAlreadyActive(sessionId);
            }

            // The previous vote's window elapsed; expire it so this new one can take its place.
            existing.Status = KickVoteStatus.Expired;
            existing.ResolvedAt = now;
        }

        string? normalizedReason = NormalizeReason(reason);
        Instant expiresAt = now.Plus(Duration.FromSeconds(KickVote.VoteDurationSeconds));

        var vote = uow.KickVoteRepository.AddKickVote(sessionId, targetMemberId, initiatorMemberId, normalizedReason, now, expiresAt);
        await uow.SaveChangesAsync();

        // The initiator implicitly approves their own kick vote.
        uow.KickVoteBallotRepository.AddBallot(vote.Id, initiatorMemberId, approve: true, now);
        await uow.SaveChangesAsync();

        logger.LogInformation("Started kick vote {VoteId} in session {SessionId}: member {InitiatorId} -> member {TargetId}", vote.Id, sessionId, initiatorMemberId, targetMemberId);

        return await ApplyBallotsAndBuildAsync(vote, targetMemberId, initiatorMemberId, now);
    }

    public async ValueTask<OneOf<IReportService.KickVoteActionResult, NotFound, DomainError>> CastBallotAsync(int sessionId, int voteId, int voterMemberId, bool approve)
    {
        var vote = await uow.KickVoteRepository.GetByIdAsync(voteId, tracking: true);
        if (vote is null || vote.SessionId != sessionId)
        {
            return new NotFound();
        }

        if (vote.Status != KickVoteStatus.Open)
        {
            return DomainError.ReportVoteNotOpen(voteId);
        }

        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            return new NotFound();
        }

        if (session.Status != SessionStatus.Active)
        {
            return DomainError.SessionNotActive(sessionId, session.Status);
        }

        var voter = await uow.TeamMemberRepository.GetMemberByIdAsync(voterMemberId, tracking: false);
        if (voter is null || voter.SessionId != sessionId)
        {
            return new NotFound();
        }

        Instant now = clock.GetCurrentInstant();

        // The window elapsed: lazily expire the vote instead of recording a late ballot.
        if (now > vote.ExpiresAt)
        {
            vote.Status = KickVoteStatus.Expired;
            vote.ResolvedAt = now;
            await uow.SaveChangesAsync();

            var expiredView = await BuildViewAsync(vote);
            return new IReportService.KickVoteActionResult(expiredView, Resolved: true, KickedMember: null, KickedMemberName: null);
        }

        var existingBallots = await uow.KickVoteBallotRepository.GetBallotsByVoteIdAsync(voteId, tracking: false);
        if (existingBallots.Any(ballot => ballot.VoterMemberId == voterMemberId))
        {
            return DomainError.ReportAlreadyVoted(voterMemberId, voteId);
        }

        uow.KickVoteBallotRepository.AddBallot(voteId, voterMemberId, approve, now);
        await uow.SaveChangesAsync();

        logger.LogInformation("Recorded ballot in kick vote {VoteId}: member {VoterId} approve={Approve}", voteId, voterMemberId, approve);

        return await ApplyBallotsAndBuildAsync(vote, vote.TargetMemberId, vote.InitiatorMemberId, now);
    }

    public async ValueTask<OneOf<IReportService.KickVoteActionResult, NotFound, DomainError>> CancelKickVoteAsync(int sessionId, int voteId, int actingMemberId)
    {
        var vote = await uow.KickVoteRepository.GetByIdAsync(voteId, tracking: true);
        if (vote is null || vote.SessionId != sessionId)
        {
            return new NotFound();
        }

        if (vote.Status != KickVoteStatus.Open)
        {
            return DomainError.ReportVoteNotOpen(voteId);
        }

        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            return new NotFound();
        }

        var acting = await uow.TeamMemberRepository.GetMemberByIdAsync(actingMemberId, tracking: false);
        if (acting is null || acting.SessionId != sessionId)
        {
            return new NotFound();
        }

        bool isInitiator = vote.InitiatorMemberId == actingMemberId;
        if (!isInitiator && !IsHost(acting, session))
        {
            return DomainError.ReportCancelNotAllowed(actingMemberId, voteId);
        }

        Instant now = clock.GetCurrentInstant();
        vote.Status = KickVoteStatus.Cancelled;
        vote.ResolvedAt = now;
        await uow.SaveChangesAsync();

        logger.LogInformation("Cancelled kick vote {VoteId} in session {SessionId} by member {ActingId}", voteId, sessionId, actingMemberId);

        var view = await BuildViewAsync(vote);
        return new IReportService.KickVoteActionResult(view, Resolved: true, KickedMember: null, KickedMemberName: null);
    }

    public async ValueTask<OneOf<IReportService.HostKickResult, NotFound, DomainError>> HostKickMemberAsync(int sessionId, int actingMemberId, int targetMemberId)
    {
        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            return new NotFound();
        }

        if (session.Status != SessionStatus.Active)
        {
            return DomainError.SessionNotActive(sessionId, session.Status);
        }

        var acting = await uow.TeamMemberRepository.GetMemberByIdAsync(actingMemberId, tracking: false);
        if (acting is null || acting.SessionId != sessionId)
        {
            return new NotFound();
        }

        if (!IsHost(acting, session))
        {
            return DomainError.ReportNotHost(actingMemberId);
        }

        var target = await uow.TeamMemberRepository.GetMemberByIdAsync(targetMemberId, tracking: true);
        if (target is null || target.SessionId != sessionId)
        {
            return new NotFound();
        }

        if (IsHost(target, session))
        {
            return DomainError.ReportTargetIsHost(targetMemberId);
        }

        Instant now = clock.GetCurrentInstant();
        string targetName = await ResolveMemberNameAsync(target);

        // If a vote was running against this member, cancel it so it does not linger after the kick.
        IReportService.KickVoteView? resolvedVoteView = null;
        var openVote = await uow.KickVoteRepository.GetOpenVoteBySessionAsync(sessionId, tracking: true);
        if (openVote is not null && openVote.TargetMemberId == targetMemberId)
        {
            openVote.Status = KickVoteStatus.Cancelled;
            openVote.ResolvedAt = now;
            resolvedVoteView = await BuildViewAsync(openVote);
        }

        uow.TeamMemberRepository.RemoveTeamMember(target);
        await uow.SaveChangesAsync();

        logger.LogInformation("Host member {ActingId} kicked member {TargetId} from session {SessionId}", actingMemberId, targetMemberId, sessionId);

        return new IReportService.HostKickResult(target, targetName, resolvedVoteView);
    }

    /// <summary>
    ///     Recompute the tally of an open vote, resolve it (passing/failing) when the outcome is
    ///     decided, remove the target on a pass, and build the resulting view.
    /// </summary>
    private async ValueTask<IReportService.KickVoteActionResult> ApplyBallotsAndBuildAsync(KickVote vote, int? targetId, int? initiatorId, Instant now)
    {
        IReadOnlyCollection<KickVoteBallot> ballots = await uow.KickVoteBallotRepository.GetBallotsByVoteIdAsync(vote.Id, tracking: false);
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionIdAsync(vote.SessionId, tracking: false);

        // Resolve names while the target member still exists in the roster (before a potential kick).
        string targetName = await ResolveMemberNameAsync(members, targetId);
        string initiatorName = await ResolveMemberNameAsync(members, initiatorId);

        int eligible = EligibleCount(members, targetId);
        int approvals = CountApprovals(ballots, targetId);
        int counted = CountedBallots(ballots, targetId);
        int needed = ApprovalsNeeded(eligible);
        int remaining = Math.Max(0, eligible - counted);

        TeamMember? kicked = null;
        if (approvals >= needed)
        {
            vote.Status = KickVoteStatus.Passed;
            vote.ResolvedAt = now;

            if (targetId is int tid)
            {
                var trackedTarget = await uow.TeamMemberRepository.GetMemberByIdAsync(tid, tracking: true);
                if (trackedTarget is not null)
                {
                    uow.TeamMemberRepository.RemoveTeamMember(trackedTarget);
                    kicked = trackedTarget;
                }
            }
        }
        else if (approvals + remaining < needed)
        {
            vote.Status = KickVoteStatus.Rejected;
            vote.ResolvedAt = now;
        }

        bool resolved = vote.Status != KickVoteStatus.Open;
        if (resolved)
        {
            await uow.SaveChangesAsync();
            logger.LogInformation("Resolved kick vote {VoteId} as {Status}", vote.Id, vote.Status);
        }

        var view = BuildView(vote, targetId, targetName, initiatorId, initiatorName, ballots, members);
        return new IReportService.KickVoteActionResult(view, resolved, kicked, kicked is null ? null : targetName);
    }

    private async ValueTask<IReportService.KickVoteView> BuildViewAsync(KickVote vote)
    {
        IReadOnlyCollection<KickVoteBallot> ballots = await uow.KickVoteBallotRepository.GetBallotsByVoteIdAsync(vote.Id, tracking: false);
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionIdAsync(vote.SessionId, tracking: false);

        string targetName = await ResolveMemberNameAsync(members, vote.TargetMemberId);
        string initiatorName = await ResolveMemberNameAsync(members, vote.InitiatorMemberId);

        return BuildView(vote, vote.TargetMemberId, targetName, vote.InitiatorMemberId, initiatorName, ballots, members);
    }

    private static IReportService.KickVoteView BuildView(
        KickVote vote,
        int? targetId,
        string targetName,
        int? initiatorId,
        string initiatorName,
        IReadOnlyCollection<KickVoteBallot> ballots,
        IReadOnlyCollection<TeamMember> members)
    {
        int eligible = EligibleCount(members, targetId);
        int approve = CountApprovals(ballots, targetId);
        int reject = CountedBallots(ballots, targetId) - approve;

        return new IReportService.KickVoteView(
            vote.Id,
            vote.SessionId,
            targetId,
            targetName,
            initiatorId,
            initiatorName,
            vote.Reason,
            vote.Status,
            approve,
            reject,
            eligible,
            vote.CreatedAt,
            vote.ExpiresAt,
            vote.ResolvedAt);
    }

    // The target cannot vote on their own kick, so their ballots never count toward the tally.
    private static int CountApprovals(IReadOnlyCollection<KickVoteBallot> ballots, int? targetId) =>
        ballots.Count(ballot => ballot.Approve && ballot.VoterMemberId != targetId);

    private static int CountedBallots(IReadOnlyCollection<KickVoteBallot> ballots, int? targetId) =>
        ballots.Count(ballot => ballot.VoterMemberId != targetId);

    // Everyone in the session may vote except the target.
    private static int EligibleCount(IReadOnlyCollection<TeamMember> members, int? targetId) =>
        members.Count(member => member.Id != targetId);

    // Strict majority of the eligible voters.
    private static int ApprovalsNeeded(int eligible) => Math.Max(eligible, 1) / 2 + 1;

    private static bool IsHost(TeamMember member, GameSession session) =>
        member.UserId.HasValue && member.UserId.Value == session.HostUserId;

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        string trimmed = reason.Trim();
        return trimmed.Length <= KickVote.MaxReasonLength ? trimmed : trimmed[..KickVote.MaxReasonLength];
    }

    private async ValueTask<string> ResolveMemberNameAsync(IReadOnlyCollection<TeamMember> members, int? memberId)
    {
        if (memberId is not int id)
        {
            return UnknownName;
        }

        var member = members.FirstOrDefault(m => m.Id == id);
        return member is null ? UnknownName : await ResolveMemberNameAsync(member);
    }

    private async ValueTask<string> ResolveMemberNameAsync(TeamMember member)
    {
        if (member.UserId is int userId)
        {
            var user = await uow.UserRepository.GetUserByIdAsync(userId, tracking: false);
            if (!string.IsNullOrWhiteSpace(user?.Username))
            {
                return user.Username;
            }
        }

        return string.IsNullOrWhiteSpace(member.GuestName) ? UnknownName : member.GuestName;
    }
}
