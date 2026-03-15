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
    private readonly TeamService _sut;
    private readonly IUnitOfWork _uow;

    public TeamServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _teamRepository = Substitute.For<ITeamRepository>();
        _uow.TeamRepository.Returns(_teamRepository);
        var logger = Substitute.For<ILogger<TeamService>>();
        _sut = new TeamService(_uow, logger);
    }

    private static Team CreateTeam(
        int id = DefaultTeamId,
        int sessionId = DefaultSessionId,
        string name = "Red Team",
        string colorCode = "#ff0000",
        TeamRole role = TeamRole.Detective
    ) =>
        new()
        {
            Id = id,
            SessionId = sessionId,
            TeamName = name,
            ColorCode = colorCode,
            Role = role,
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

        _teamRepository.AddTeam(data.SessionId, data.TeamName, data.Role, data.ColorCode).Returns(team);

        OneOf<Team, Error> result = await _sut.AddTeamAsync(data);

        result.Switch(
            team => team.Should().BeEquivalentTo(team),
            error => Assert.Fail("Expected a team but got an Error")
        );
        team.IsCaught.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsNotFound_WhenMismatchSession()
    {
        var team = CreateTeam(DefaultTeamId, 99, "Red Team", "#ff0000", TeamRole.Detective);
        var data = new ITeamService.TeamData(DefaultSessionId, "Red Team", TeamRole.Detective, "#ff0000");

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);

        OneOf<Success, NotFound> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask UpdateTeamAsync_ReturnsSuccess_WhenFound()
    {
        var team = CreateTeam(DefaultTeamId, DefaultSessionId, "Old Name", "#ff0000", TeamRole.Detective);
        team.IsCaught = false;
        var data = new ITeamService.TeamData(DefaultSessionId, "New Name", TeamRole.Detective, "#00ff00", true);

        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);

        OneOf<Success, NotFound> result = await _sut.UpdateTeamAsync(DefaultSessionId, DefaultTeamId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        team.TeamName.Should().Be("New Name");
        team.ColorCode.Should().Be("#00ff00");
        team.IsCaught.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask DeleteTeamAsync_ReturnsNotFound_WhenMismatchSession()
    {
        var team = CreateTeam(DefaultTeamId, 99, "Red Team", "#ff0000", TeamRole.Detective);
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);

        OneOf<Success, NotFound> result = await _sut.DeleteTeamAsync(DefaultSessionId, DefaultTeamId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask DeleteTeamAsync_ReturnsSuccess_WhenFound()
    {
        var team = CreateTeam();
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, true).Returns(team);

        OneOf<Success, NotFound> result = await _sut.DeleteTeamAsync(DefaultSessionId, DefaultTeamId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        _teamRepository.Received(1).RemoveTeam(team);
        await _uow.Received(1).SaveChangesAsync();
    }
}
