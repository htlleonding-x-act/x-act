using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Threading.Tasks;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Repositories;
using XActBackend.Persistence.Util;

namespace XActBackend.Test;

public sealed class TeamMemberServiceTests
{
    private const int DefaultSessionId = 1;
    private const int DefaultTeamId = 1;
    private const int DefaultMemberId = 1;
    private const int DefaultUserId = 10;

    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly TeamMemberService _sut;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public TeamMemberServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _teamRepository = Substitute.For<ITeamRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
        _uow.GameSessionRepository.Returns(_gameSessionRepository);
        _uow.TeamRepository.Returns(_teamRepository);
        _uow.UserRepository.Returns(_userRepository);

        _clock = Substitute.For<IClock>();
        var logger = Substitute.For<ILogger<TeamMemberService>>();
        _sut = new TeamMemberService(_uow, _clock, logger);
    }

    private static TeamMember CreateMember(
        int id = DefaultMemberId,
        int sessionId = DefaultSessionId,
        int teamId = DefaultTeamId,
        int? userId = DefaultUserId
    ) =>
        new()
        {
            Id = id,
            SessionId = sessionId,
            TeamId = teamId,
            UserId = userId,
        };

    private static GameSession CreateWaitingSession() =>
        new()
        {
            Id = DefaultSessionId,
            SessionName = "Waiting Session",
            JoinCode = "WAIT01",
            Status = SessionStatus.Waiting,
        };

    private static GameSession CreateActiveSession() =>
        new()
        {
            Id = DefaultSessionId,
            SessionName = "Active Session",
            JoinCode = "ACT001",
            Status = SessionStatus.Active,
        };

    private static Team CreateTeam() =>
        new()
        {
            Id = DefaultTeamId,
            SessionId = DefaultSessionId,
            TeamName = "Detectives",
            Role = TeamRole.Detective,
            ColorCode = "#ff0000",
        };

    private static User CreateUser(int id = DefaultUserId, bool isDeleted = false) =>
        new()
        {
            Id = id,
            IsDeleted = isDeleted,
        };

    [Fact]
    internal async ValueTask GetMembersByTeamIdAsync_ReturnsMembers()
    {
        var members = new List<TeamMember>
        {
            CreateMember(DefaultMemberId, DefaultSessionId, DefaultTeamId),
            CreateMember(2, DefaultSessionId, DefaultTeamId)
        };
        _teamMemberRepository.GetMembersBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, false).Returns(members);

        var result = await _sut.GetMembersByTeamIdAsync(DefaultSessionId, DefaultTeamId, false);

        result.Should().BeEquivalentTo(members);
    }

    [Fact]
    internal async ValueTask GetTeamMemberByIdAsync_ReturnsMember_WhenFound()
    {
        var member = CreateMember();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(member);

        OneOf<TeamMember, NotFound> result = await _sut.GetTeamMemberByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false);

        result.Switch(
            teamMember => teamMember.Should().BeEquivalentTo(member),
            notFound => Assert.Fail("Expected TeamMember but got NotFound")
        );
    }

    [Fact]
    internal async ValueTask GetTeamMemberByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns((TeamMember?) null);

        OneOf<TeamMember, NotFound> result = await _sut.GetTeamMemberByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false);

        result.Switch(
            teamMember => Assert.Fail("Expected NotFound but got TeamMember"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsDomainError_WhenNeitherUserIdNorGuestNameProvided()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, null, null);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            teamMember => Assert.Fail("Expected DomainError but got TeamMember"),
            notFound => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.InvalidMemberIdentity)
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsDomainError_WhenUserAlreadyInSession()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        var existingMember = CreateMember(2, DefaultSessionId, DefaultTeamId, DefaultUserId);

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _teamMemberRepository.GetMemberBySessionAndUserIdAsync(DefaultSessionId, DefaultUserId, false).Returns(existingMember);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            teamMember => Assert.Fail("Expected DomainError but got TeamMember"),
            notFound => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.UserAlreadyJoined)
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsNotFound_WhenSessionMissing()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got TeamMember"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsDomainError_WhenSessionNotJoinable()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got TeamMember"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotJoinable)
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsNotFound_WhenTeamMissing()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns((Team?) null);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got TeamMember"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsDomainError_WhenTeamNotInSession()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(
            new Team { Id = DefaultTeamId, SessionId = 99, TeamName = "Other", Role = TeamRole.Detective, ColorCode = "#ffffff" });

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got TeamMember"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.TeamNotInSession)
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsNotFound_WhenUserMissing()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns((User?) null);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got TeamMember"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsDomainError_WhenUserDeleted()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser(DefaultUserId, isDeleted: true));

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got TeamMember"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.UserDeleted)
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsDomainError_WhenLeaderAlreadyExists()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null, IsTeamLeader: true);

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _teamMemberRepository.GetMemberBySessionAndUserIdAsync(DefaultSessionId, DefaultUserId, false).Returns((TeamMember?) null);
        _teamMemberRepository.GetMembersBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, false)
            .Returns([new TeamMember { Id = 99, IsTeamLeader = true, SessionId = DefaultSessionId, TeamId = DefaultTeamId }]);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got TeamMember"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.TeamLeaderAlreadyExists)
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsAddedMember()
    {
        var lastUpdated = SystemClock.Instance.GetCurrentInstant();
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null, true, 45.0, 90.0, lastUpdated);
        var member = CreateMember(DefaultMemberId, DefaultSessionId, DefaultTeamId, DefaultUserId);
        member.IsTeamLeader = true;

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _teamMemberRepository.GetMemberBySessionAndUserIdAsync(DefaultSessionId, DefaultUserId, false).Returns((TeamMember?) null);
        _teamMemberRepository.GetMembersBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, false).Returns([]);
        _teamMemberRepository.AddTeamMember(DefaultSessionId, DefaultTeamId, DefaultUserId, null, true).Returns(member);

        OneOf<TeamMember, NotFound, DomainError> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            teamMember => teamMember.Should().BeEquivalentTo(member),
            notFound => Assert.Fail("Expected TeamMember but got NotFound"),
            domainError => Assert.Fail("Expected TeamMember but got DomainError")
        );
        member.CurrentLatitude.Should().Be(45.0);
        member.CurrentLongitude.Should().Be(90.0);
        member.LastUpdated.Should().Be(lastUpdated);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask UpdateTeamMemberAsync_ReturnsNotFound_WhenMismatchSessionOrTeam()
    {
        var member = CreateMember(DefaultMemberId, 99, 99, null);
        var data = new ITeamMemberService.TeamMemberData(2, 2, null, "Guest");

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true).Returns(member);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamMemberAsync_ReturnsSuccess_WhenFound()
    {
        var member = CreateMember(DefaultMemberId, DefaultSessionId, DefaultTeamId, null);
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, null, "New Guest");
        var now = SystemClock.Instance.GetCurrentInstant();
        _clock.GetCurrentInstant().Returns(now);

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true).Returns(member);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail("Expected Success but got DomainError")
        );
        member.GuestName.Should().Be("New Guest");
        member.LastUpdated.Should().Be(now);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask UpdateTeamMemberAsync_ReturnsDomainError_WhenIdentityIsInvalid()
    {
        var member = CreateMember(DefaultMemberId, DefaultSessionId, DefaultTeamId, null);
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, "Guest");
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true).Returns(member);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.InvalidMemberIdentity)
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamMemberAsync_ReturnsDomainError_WhenLeaderAlreadyExists()
    {
        var member = CreateMember(DefaultMemberId, DefaultSessionId, DefaultTeamId, DefaultUserId);
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null, IsTeamLeader: true);

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true).Returns(member);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateTeam());
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _teamMemberRepository.GetMemberBySessionAndUserIdAsync(DefaultSessionId, DefaultUserId, false).Returns(member);
        _teamMemberRepository.GetMembersBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, false)
            .Returns(
            [
                new TeamMember { Id = DefaultMemberId, IsTeamLeader = false, SessionId = DefaultSessionId, TeamId = DefaultTeamId },
                new TeamMember { Id = 99, IsTeamLeader = true, SessionId = DefaultSessionId, TeamId = DefaultTeamId },
            ]);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.TeamLeaderAlreadyExists)
        );
    }

    [Fact]
    internal async ValueTask DeleteTeamMemberAsync_ReturnsSuccess_WhenFound()
    {
        var member = CreateMember();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true).Returns(member);

        OneOf<Success, NotFound> result = await _sut.DeleteTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        _teamMemberRepository.Received(1).RemoveTeamMember(member);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask DeleteTeamMemberAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true).Returns((TeamMember?) null);

        OneOf<Success, NotFound> result = await _sut.DeleteTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }
}
