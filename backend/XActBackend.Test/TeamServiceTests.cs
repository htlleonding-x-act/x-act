using AwesomeAssertions;
using Microsoft.Extensions.Logging;
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

public sealed class TeamServiceTests
{
    private const int DefaultSessionId = 1;
    private const int DefaultTeamId = 1;

    private readonly ITeamRepository _teamRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly TeamService _sut;
    private readonly IUnitOfWork _uow;

    public TeamServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _teamRepository = Substitute.For<ITeamRepository>();
        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _uow.TeamRepository.Returns(_teamRepository);
        _uow.GameSessionRepository.Returns(_gameSessionRepository);
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
        var logger = Substitute.For<ILogger<TeamService>>();
        _sut = new TeamService(_uow, logger);
    }

    private static GameSession CreateWaitingSession(int sessionId = DefaultSessionId) =>
        new()
        {
            Id = sessionId,
            HostUserId = "1",
            SessionName = "Waiting Session",
            JoinCode = "WAIT01",
            Status = SessionStatus.Waiting,
        };

    private static Team CreateTeam(
        int id = DefaultTeamId,
        int sessionId = DefaultSessionId,
        string name = "Red Team",
        string colorCode = "#ff0000",
        TeamRole role = TeamRole.Detective,
        int maxPlayerCount = Team.DefaultMaxPlayerCount
    ) =>
        new()
        {
            Id = id,
            SessionId = sessionId,
            TeamName = name,
            ColorCode = colorCode,
            Role = role,
            MaxPlayerCount = maxPlayerCount,
        };

    private static List<Team> CreateTeams() =>
        [
            CreateTeam(DefaultTeamId, DefaultSessionId, "Red Team", "#ff0000"),
            CreateTeam(2, DefaultSessionId, "Blue Team", "#0000ff"),
        ];

    [Fact]
    internal async ValueTask GetTeamsBySessionIdAsync_ReturnsTeams()
    {
        var teams = CreateTeams();
        _teamRepository.GetTeamsBySessionIdAsync(DefaultSessionId, false).Returns(teams);

        var result = await _sut.GetTeamsBySessionIdAsync(DefaultSessionId, false);

        result.Should().BeEquivalentTo(teams);
    }

    [Fact]
    internal async ValueTask GetTeamByIdAsync_ReturnsTeam_WhenFoundAndSessionMatches()
    {
        var team = CreateTeam();
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(team);

        OneOf<Team, NotFound> result = await _sut.GetTeamByIdAsync(DefaultSessionId, DefaultTeamId, false);

        result.Switch(
            found => found.Should().BeEquivalentTo(team),
            _ => Assert.Fail("Expected a team but got NotFound")
        );
    }

    [Fact]
    internal async ValueTask GetTeamByIdAsync_ReturnsNotFound_WhenMismatchSession()
    {
        var team = CreateTeam(DefaultTeamId, 99, "Red Team", "#ff0000");
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(team);

        OneOf<Team, NotFound> result = await _sut.GetTeamByIdAsync(DefaultSessionId, DefaultTeamId, false);

        result.Switch(
            team => Assert.Fail("Expected NotFound but got a team"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask GetTeamByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns((Team?) null);

        OneOf<Team, NotFound> result = await _sut.GetTeamByIdAsync(DefaultSessionId, DefaultTeamId, false);

        result.Switch(
            team => Assert.Fail("Expected NotFound but got a team"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask AddTeamAsync_ReturnsAddedTeam()
    {
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#ff0000", true);
        var team = CreateTeam(DefaultTeamId, DefaultSessionId, "Red Team", "#ff0000", TeamRole.Detective);

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamBySessionAndRoleAsync(DefaultSessionId, TeamRole.MrX, false).Returns((Team?) null);
        _teamRepository.AddTeam(data.SessionId, data.TeamName, data.Role, data.ColorCode, data.MaxPlayerCount).Returns(team);

        OneOf<Team, NotFound, DomainError> result = await _sut.AddTeamAsync(data);

        result.Switch(
            addedTeam => addedTeam.Should().BeEquivalentTo(team),
            notFound => Assert.Fail("Expected a team but got NotFound"),
            domainError => Assert.Fail("Expected a team but got DomainError")
        );
        team.IsCaught.Should().BeTrue();
        team.MaxPlayerCount.Should().Be(Team.DefaultMaxPlayerCount);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask AddTeamAsync_ReturnsNotFound_WhenSessionMissing()
    {
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#ff0000");
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<Team, NotFound, DomainError> result = await _sut.AddTeamAsync(data);

        result.Switch(
            team => Assert.Fail("Expected NotFound but got Team"),
            _ => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask AddTeamAsync_ReturnsDomainError_WhenSessionIsNotWaiting()
    {
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#ff0000");
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(
            new GameSession
            {
                Id = DefaultSessionId,
                HostUserId = "1",
                SessionName = "Active",
                JoinCode = "ACTIVE1",
                Status = SessionStatus.Active,
            });

        OneOf<Team, NotFound, DomainError> result = await _sut.AddTeamAsync(data);

        result.Switch(
            team => Assert.Fail("Expected DomainError but got Team"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotJoinable)
        );
    }

    [Fact]
    internal async ValueTask AddTeamAsync_ReturnsDomainError_WhenMrXTeamAlreadyExists()
    {
        var data = new ITeamService.TeamData(DefaultSessionId, "Another MrX", TeamRole.MrX, "#111111");
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamBySessionAndRoleAsync(DefaultSessionId, TeamRole.MrX, false)
            .Returns(CreateTeam(99, DefaultSessionId, "Existing MrX", "#000000", TeamRole.MrX));

        OneOf<Team, NotFound, DomainError> result = await _sut.AddTeamAsync(data);

        result.Switch(
            team => Assert.Fail("Expected DomainError but got Team"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.MrXTeamAlreadyExists)
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsNotFound_WhenMismatchSession()
    {
        var team = CreateTeam(DefaultTeamId, 99, "Red Team", "#ff0000", TeamRole.Detective);
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#ff0000");

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsSuccess_WhenFound()
    {
        var team = CreateTeam(DefaultTeamId, DefaultSessionId, "Old Name", "#ff0000", TeamRole.Detective);
        team.IsCaught = false;
        var data = new ITeamService.TeamData(DefaultSessionId, "New Name", TeamRole.Detective, "#00ff00", true, 8);

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail("Expected Success but got DomainError")
        );
        team.TeamName.Should().Be("New Name");
        team.ColorCode.Should().Be("#00ff00");
        team.IsCaught.Should().BeTrue();
        team.MaxPlayerCount.Should().Be(8);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        var team = CreateTeam(DefaultTeamId, DefaultSessionId, "Red Team", "#ff0000", TeamRole.Detective);
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#00ff00");

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsDomainError_WhenSessionIsNotWaiting()
    {
        var team = CreateTeam(DefaultTeamId, DefaultSessionId, "Red Team", "#ff0000", TeamRole.Detective);
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#00ff00");

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(
            new GameSession
            {
                Id = DefaultSessionId,
                HostUserId = "1",
                SessionName = "Active",
                JoinCode = "ACTIVE1",
                Status = SessionStatus.Active,
            });

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotJoinable)
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsDomainError_WhenOtherMrXTeamExists()
    {
        var team = CreateTeam(DefaultTeamId, DefaultSessionId, "Red Team", "#ff0000", TeamRole.Detective);
        var data = new ITeamService.TeamData(DefaultSessionId, "MrX Team", TeamRole.MrX, "#000000");

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());
        _teamRepository.GetTeamBySessionAndRoleAsync(DefaultSessionId, TeamRole.MrX, false)
            .Returns(CreateTeam(42, DefaultSessionId, "Existing MrX", "#000000", TeamRole.MrX));

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.MrXTeamAlreadyExists)
        );
    }

    [Fact]
    internal async ValueTask DeleteTeamAsync_ReturnsNotFound_WhenMismatchSession()
    {
        var team = CreateTeam(DefaultTeamId, 99, "Red Team", "#ff0000", TeamRole.Detective);
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);

        OneOf<Success, NotFound, DomainError> result = await _sut.DeleteTeamAsync(DefaultSessionId, DefaultTeamId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask DeleteTeamAsync_ReturnsSuccess_WhenFound()
    {
        var team = CreateTeam();
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);
        _teamMemberRepository.GetMembersBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, false).Returns([]);

        OneOf<Success, NotFound, DomainError> result = await _sut.DeleteTeamAsync(DefaultSessionId, DefaultTeamId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail("Expected Success but got DomainError")
        );
        _teamRepository.Received(1).RemoveTeam(team);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask DeleteTeamAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns((Team?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.DeleteTeamAsync(DefaultSessionId, DefaultTeamId, true);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask DeleteTeamAsync_ReturnsDomainError_WhenTeamStillHasMembers()
    {
        var team = CreateTeam();
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);
        _teamMemberRepository.GetMembersBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, false)
            .Returns([new TeamMember { Id = 10, SessionId = DefaultSessionId, TeamId = DefaultTeamId }]);

        OneOf<Success, NotFound, DomainError> result = await _sut.DeleteTeamAsync(DefaultSessionId, DefaultTeamId, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.TeamHasMembers)
        );
        await _uow.DidNotReceive().SaveChangesAsync();
    }
}
