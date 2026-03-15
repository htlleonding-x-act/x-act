using AwesomeAssertions;
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

public sealed class GameSessionServiceTests
{
    private const int DefaultSessionId = 1;
    private const int DefaultUserId = 1;
    private const string DefaultSessionName = "Game 1";
    private const string DefaultJoinCode = "JOIN123";

    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly GameSessionService _sut;
    private readonly IUnitOfWork _uow;

    public GameSessionServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();

        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _uow.GameSessionRepository.Returns(_gameSessionRepository);

        _userRepository = Substitute.For<IUserRepository>();
        _uow.UserRepository.Returns(_userRepository);

        _teamRepository = Substitute.For<ITeamRepository>();
        _uow.TeamRepository.Returns(_teamRepository);

        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);

        _sut = new GameSessionService(_uow);
    }

    private static GameSession CreateSession(
        int id = DefaultSessionId,
        string? name = null,
        string? joinCode = null
    ) =>
        new()
        {
            Id = id,
            SessionName = name ?? DefaultSessionName,
            JoinCode = joinCode ?? DefaultJoinCode,
        };

    private static List<GameSession> CreateSessions() =>
        [
            CreateSession(DefaultSessionId, DefaultSessionName, DefaultJoinCode),
            CreateSession(2, "Game 2", "JOIN456"),
        ];

    private static User CreateUser(int id = DefaultUserId) =>
        new()
        {
            Id = id,
            IsDeleted = false,
        };

    private static Team CreateTeam(
        int id,
        string name,
        TeamRole role,
        string colorCode
    ) =>
        new()
        {
            Id = id,
            TeamName = name,
            Role = role,
            ColorCode = colorCode,
        };

    [Fact]
    public async ValueTask GetAllGameSessionsAsync_ReturnsSessions()
    {
        var sessions = CreateSessions();
        _gameSessionRepository.GetAllSessionsAsync(false).Returns(sessions);

        var result = await _sut.GetAllGameSessionsAsync(false);

        result.Should().BeEquivalentTo(sessions);
    }

    [Fact]
    public async ValueTask GetGameSessionByIdAsync_ReturnsSession_WhenFound()
    {
        var session = CreateSession();
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(session);

        OneOf<GameSession, NotFound> result = await _sut.GetGameSessionByIdAsync(DefaultSessionId, false);

        result.Switch(
            found => found.Should().BeEquivalentTo(session),
            notFound => Assert.Fail("Expected GameSession but got NotFound")
        );
    }

    [Fact]
    public async ValueTask GetGameSessionByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<GameSession, NotFound> result = await _sut.GetGameSessionByIdAsync(DefaultSessionId, false);

        result.Switch(
            session => Assert.Fail("Expected NotFound but got GameSession"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask AddGameSessionAsync_ReturnsError_WhenHostUserNotFound()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns((User?) null);

        OneOf<GameSession, Error> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            session => Assert.Fail("Expected Error but got GameSession"),
            error => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask AddGameSessionAsync_ReturnsAddedSession_WhenValid()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        var user = CreateUser();
        var session = CreateSession(10, data.SessionName, data.JoinCode);
        var team = CreateTeam(20, "Game 1 Host", TeamRole.MrX, "#000000");

        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(user);
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns((GameSession?) null);
        _gameSessionRepository.AddGameSession(DefaultUserId, DefaultSessionName, DefaultJoinCode, 60, 5).Returns(session);
        _teamRepository.AddTeam(session.Id, "Game 1 Host", TeamRole.MrX, "#000000").Returns(team);

        OneOf<GameSession, Error> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            added => added.Should().BeEquivalentTo(session),
            error => Assert.Fail("Expected GameSession but got Error")
        );
        _teamMemberRepository.Received(1).AddTeamMember(session.Id, team.Id, user.Id, null, true);
        await _uow.Received(3).SaveChangesAsync();
    }
}