using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>kick votes and host kicks. the host can never be the target of either</summary>
public interface IReportService
{
    /// <summary>null when no vote is open or the open one has run past its window</summary>
    public ValueTask<KickVoteView?> GetOpenVoteAsync(int sessionId);

    /// <summary>the initiator's approval is counted right away</summary>
    public ValueTask<OneOf<KickVoteActionResult, NotFound, DomainError>> StartKickVoteAsync(int sessionId, int initiatorMemberId, int targetMemberId, string? reason);

    /// <summary>resolves the vote as soon as the outcome is decided</summary>
    public ValueTask<OneOf<KickVoteActionResult, NotFound, DomainError>> CastBallotAsync(int sessionId, int voteId, int voterMemberId, bool approve);

    /// <summary>only the initiator or the host may cancel</summary>
    public ValueTask<OneOf<KickVoteActionResult, NotFound, DomainError>> CancelKickVoteAsync(int sessionId, int voteId, int actingMemberId);

    /// <summary>kicks right away without a vote. only the host can do this</summary>
    public ValueTask<OneOf<HostKickResult, NotFound, DomainError>> HostKickMemberAsync(int sessionId, int actingMemberId, int targetMemberId);

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

    public sealed record KickVoteActionResult(
        KickVoteView Vote,
        bool Resolved,
        TeamMember? KickedMember,
        string? KickedMemberName
    );

    /// <param name="ResolvedVote">an open vote against the same member that the kick cancelled</param>
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

        // a vote past its window counts as closed here but only gets marked expired on the next start or ballot
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

            // the old vote ran out, so expire it and let the new one take its place
            existing.Status = KickVoteStatus.Expired;
            existing.ResolvedAt = now;
        }

        string? normalizedReason = NormalizeReason(reason);
        Instant expiresAt = now.Plus(Duration.FromSeconds(KickVote.VoteDurationSeconds));

        var vote = uow.KickVoteRepository.AddKickVote(sessionId, targetMemberId, initiatorMemberId, normalizedReason, now, expiresAt);
        await uow.SaveChangesAsync();

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

        // the ballot came too late, so expire the vote instead of counting it
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

        // cancel a running vote against this member so it doesn't hang around after the kick
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

    // recounts the ballots, resolves the vote once the outcome is decided and removes the target if it passed
    private async ValueTask<IReportService.KickVoteActionResult> ApplyBallotsAndBuildAsync(KickVote vote, int? targetId, int? initiatorId, Instant now)
    {
        IReadOnlyCollection<KickVoteBallot> ballots = await uow.KickVoteBallotRepository.GetBallotsByVoteIdAsync(vote.Id, tracking: false);
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionIdAsync(vote.SessionId, tracking: false);

        // look up names before a possible kick removes the target from the roster
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

    // the target can't vote on their own kick, so their ballots never count
    private static int CountApprovals(IReadOnlyCollection<KickVoteBallot> ballots, int? targetId) =>
        ballots.Count(ballot => ballot.Approve && ballot.VoterMemberId != targetId);

    private static int CountedBallots(IReadOnlyCollection<KickVoteBallot> ballots, int? targetId) =>
        ballots.Count(ballot => ballot.VoterMemberId != targetId);

    private static int EligibleCount(IReadOnlyCollection<TeamMember> members, int? targetId) =>
        members.Count(member => member.Id != targetId);

    // strict majority, and at least one approval even when nobody is eligible
    private static int ApprovalsNeeded(int eligible) => Math.Max(eligible, 1) / 2 + 1;

    private static bool IsHost(TeamMember member, GameSession session) =>
        member.UserId is not null && member.UserId == session.HostUserId;

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
        if (member.UserId is { } userId)
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
