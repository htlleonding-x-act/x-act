using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Repositories;
using XActBackend.Persistence.Util;

namespace XActBackend.Test;

public sealed class ReportServiceTests
{
    private const int SessionId = 1;
    private const int TeamId = 2;
    private const int HostUserId = 5;
    private const int HostMemberId = 100;
    private const int InitiatorId = 10;
    private const int TargetId = 20;
    private const int VoteId = 77;

    private static readonly Instant now = Instant.FromUtc(2026, 1, 1, 12, 0);

    private readonly IKickVoteRepository _voteRepository;
    private readonly IKickVoteBallotRepository _ballotRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _uow;
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _voteRepository = Substitute.For<IKickVoteRepository>();
        _ballotRepository = Substitute.For<IKickVoteBallotRepository>();
        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _userRepository = Substitute.For<IUserRepository>();

        _uow.KickVoteRepository.Returns(_voteRepository);
        _uow.KickVoteBallotRepository.Returns(_ballotRepository);
        _uow.GameSessionRepository.Returns(_gameSessionRepository);
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
        _uow.UserRepository.Returns(_userRepository);
        _uow.SaveChangesAsync().Returns(Task.CompletedTask);

        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);

        var logger = Substitute.For<ILogger<ReportService>>();
        _sut = new ReportService(_uow, clock, logger);
    }

    private static GameSession CreateSession(SessionStatus status = SessionStatus.Active) =>
        new() { Id = SessionId, SessionName = "S", JoinCode = "ABCDE1", Status = status, HostUserId = HostUserId };

    private static TeamMember HostMember() =>
        new() { Id = HostMemberId, SessionId = SessionId, TeamId = TeamId, UserId = HostUserId };

    private static TeamMember GuestMember(int id, string name) =>
        new() { Id = id, SessionId = SessionId, TeamId = TeamId, GuestName = name };

    private static KickVote OpenVote(Instant expiresAt) =>
        new()
        {
            Id = VoteId,
            SessionId = SessionId,
            TargetMemberId = TargetId,
            InitiatorMemberId = InitiatorId,
            Status = KickVoteStatus.Open,
            CreatedAt = now,
            ExpiresAt = expiresAt,
        };

    private static KickVoteBallot Ballot(int voterId, bool approve) =>
        new() { KickVoteId = VoteId, VoterMemberId = voterId, Approve = approve, CastAt = now };

    [Fact]
    public async ValueTask StartKickVote_ReturnsDomainError_WhenTargetIsHost()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(InitiatorId, false).Returns(GuestMember(InitiatorId, "Init"));
        _teamMemberRepository.GetMemberByIdAsync(HostMemberId, false).Returns(HostMember());

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.StartKickVoteAsync(SessionId, InitiatorId, HostMemberId, reason: null);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportTargetIsHost);
    }

    [Fact]
    public async ValueTask StartKickVote_ReturnsDomainError_WhenTargetIsSelf()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(InitiatorId, false).Returns(GuestMember(InitiatorId, "Init"));

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.StartKickVoteAsync(SessionId, InitiatorId, InitiatorId, reason: null);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportTargetIsSelf);
    }

    [Fact]
    public async ValueTask StartKickVote_ReturnsDomainError_WhenSessionNotActive()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession(SessionStatus.Waiting));

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.StartKickVoteAsync(SessionId, InitiatorId, TargetId, reason: null);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.SessionNotActive);
    }

    [Fact]
    public async ValueTask StartKickVote_ReturnsDomainError_WhenVoteAlreadyActive()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(InitiatorId, false).Returns(GuestMember(InitiatorId, "Init"));
        _teamMemberRepository.GetMemberByIdAsync(TargetId, false).Returns(GuestMember(TargetId, "Target"));
        _voteRepository.GetOpenVoteBySessionAsync(SessionId, true).Returns(OpenVote(now.Plus(Duration.FromSeconds(30))));

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.StartKickVoteAsync(SessionId, InitiatorId, TargetId, reason: null);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportVoteAlreadyActive);
    }

    [Fact]
    public async ValueTask StartKickVote_StartsOpenVote_WithInitiatorApproval()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(InitiatorId, false).Returns(GuestMember(InitiatorId, "Init"));
        _teamMemberRepository.GetMemberByIdAsync(TargetId, false).Returns(GuestMember(TargetId, "Target"));
        _voteRepository.GetOpenVoteBySessionAsync(SessionId, true).Returns((KickVote?)null);
        _voteRepository
            .AddKickVote(SessionId, TargetId, InitiatorId, Arg.Any<string?>(), Arg.Any<Instant>(), Arg.Any<Instant>())
            .Returns(OpenVote(now.Plus(Duration.FromSeconds(KickVote.VoteDurationSeconds))));
        // Three members -> two eligible voters (host + initiator) -> needs 2 approvals, so one is not enough.
        _teamMemberRepository.GetMembersBySessionIdAsync(SessionId, false)
            .Returns(new List<TeamMember> { HostMember(), GuestMember(InitiatorId, "Init"), GuestMember(TargetId, "Target") });
        _ballotRepository.GetBallotsByVoteIdAsync(VoteId, false)
            .Returns(new List<KickVoteBallot> { Ballot(InitiatorId, approve: true) });

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.StartKickVoteAsync(SessionId, InitiatorId, TargetId, reason: "left the area");

        result.TryPickT0(out var action, out _).Should().BeTrue();
        action.Resolved.Should().BeFalse();
        action.KickedMember.Should().BeNull();
        action.Vote.Status.Should().Be(KickVoteStatus.Open);
        action.Vote.ApproveCount.Should().Be(1);
        action.Vote.EligibleVoterCount.Should().Be(2);
    }

    [Fact]
    public async ValueTask CastBallot_ReturnsDomainError_WhenAlreadyVoted()
    {
        _voteRepository.GetByIdAsync(VoteId, true).Returns(OpenVote(now.Plus(Duration.FromSeconds(30))));
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(HostMemberId, false).Returns(HostMember());
        _ballotRepository.GetBallotsByVoteIdAsync(VoteId, false)
            .Returns(new List<KickVoteBallot> { Ballot(HostMemberId, approve: true) });

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.CastBallotAsync(SessionId, VoteId, HostMemberId, approve: true);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportAlreadyVoted);
    }

    [Fact]
    public async ValueTask CastBallot_ReturnsDomainError_WhenVoteNotOpen()
    {
        var resolvedVote = OpenVote(now.Plus(Duration.FromSeconds(30)));
        resolvedVote.Status = KickVoteStatus.Passed;
        _voteRepository.GetByIdAsync(VoteId, true).Returns(resolvedVote);

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.CastBallotAsync(SessionId, VoteId, HostMemberId, approve: true);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportVoteNotOpen);
    }

    [Fact]
    public async ValueTask CastBallot_PassesAndKicksTarget_WhenMajorityApproves()
    {
        _voteRepository.GetByIdAsync(VoteId, true).Returns(OpenVote(now.Plus(Duration.FromSeconds(30))));
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(HostMemberId, false).Returns(HostMember());
        // Two eligible voters (host + initiator); both approve -> passes.
        _teamMemberRepository.GetMembersBySessionIdAsync(SessionId, false)
            .Returns(new List<TeamMember> { HostMember(), GuestMember(InitiatorId, "Init"), GuestMember(TargetId, "Target") });
        _teamMemberRepository.GetMemberByIdAsync(TargetId, true).Returns(GuestMember(TargetId, "Target"));
        // First call: duplicate check (only initiator). Second call: tally after host's ballot.
        _ballotRepository.GetBallotsByVoteIdAsync(VoteId, false).Returns(
            new List<KickVoteBallot> { Ballot(InitiatorId, approve: true) },
            new List<KickVoteBallot> { Ballot(InitiatorId, approve: true), Ballot(HostMemberId, approve: true) });

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.CastBallotAsync(SessionId, VoteId, HostMemberId, approve: true);

        result.TryPickT0(out var action, out _).Should().BeTrue();
        action.Resolved.Should().BeTrue();
        action.Vote.Status.Should().Be(KickVoteStatus.Passed);
        action.KickedMember.Should().NotBeNull();
        action.KickedMember!.Id.Should().Be(TargetId);
        _teamMemberRepository.Received(1).RemoveTeamMember(Arg.Is<TeamMember>(m => m.Id == TargetId));
    }

    [Fact]
    public async ValueTask CancelKickVote_ReturnsDomainError_WhenNotInitiatorOrHost()
    {
        const int strangerId = 30;
        _voteRepository.GetByIdAsync(VoteId, true).Returns(OpenVote(now.Plus(Duration.FromSeconds(30))));
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(strangerId, false).Returns(GuestMember(strangerId, "Stranger"));

        OneOf<IReportService.KickVoteActionResult, NotFound, DomainError> result =
            await _sut.CancelKickVoteAsync(SessionId, VoteId, strangerId);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportCancelNotAllowed);
    }

    [Fact]
    public async ValueTask HostKick_ReturnsDomainError_WhenActingMemberNotHost()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(InitiatorId, false).Returns(GuestMember(InitiatorId, "Init"));

        OneOf<IReportService.HostKickResult, NotFound, DomainError> result =
            await _sut.HostKickMemberAsync(SessionId, InitiatorId, TargetId);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportNotHost);
    }

    [Fact]
    public async ValueTask HostKick_ReturnsDomainError_WhenTargetIsHost()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(HostMemberId, false).Returns(HostMember());
        _teamMemberRepository.GetMemberByIdAsync(HostMemberId, true).Returns(HostMember());

        OneOf<IReportService.HostKickResult, NotFound, DomainError> result =
            await _sut.HostKickMemberAsync(SessionId, HostMemberId, HostMemberId);

        result.TryPickT2(out var error, out _).Should().BeTrue();
        error.Code.Should().Be(DomainErrorCodes.ReportTargetIsHost);
    }

    [Fact]
    public async ValueTask HostKick_RemovesTarget_WhenInvokedByHost()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(HostMemberId, false).Returns(HostMember());
        _teamMemberRepository.GetMemberByIdAsync(TargetId, true).Returns(GuestMember(TargetId, "Target"));
        _voteRepository.GetOpenVoteBySessionAsync(SessionId, true).Returns((KickVote?)null);

        OneOf<IReportService.HostKickResult, NotFound, DomainError> result =
            await _sut.HostKickMemberAsync(SessionId, HostMemberId, TargetId);

        result.TryPickT0(out var hostKick, out _).Should().BeTrue();
        hostKick.KickedMember.Id.Should().Be(TargetId);
        hostKick.KickedMemberName.Should().Be("Target");
        _teamMemberRepository.Received(1).RemoveTeamMember(Arg.Is<TeamMember>(m => m.Id == TargetId));
    }
}
