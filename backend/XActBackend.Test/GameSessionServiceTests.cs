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

public sealed class GameSessionServiceTests
{
    private const int DefaultSessionId = 1;
    private const string DefaultUserId = "1";
    private const string DefaultSessionName = "Game 1";
    private const string DefaultJoinCode = "JOIN123";

    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly GameSessionService _sut;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

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

        _clock = Substitute.For<IClock>();
        var logger = Substitute.For<ILogger<GameSessionService>>();
        _sut = new GameSessionService(_uow, _clock, logger);
    }

    private static GameSession CreateSession(
        int id = DefaultSessionId,
        string? name = null,
        string? joinCode = null
    ) =>
        new()
        {
            Id = id,
            HostUserId = DefaultUserId,
            SessionName = name ?? DefaultSessionName,
            JoinCode = joinCode ?? DefaultJoinCode,
        };

    private static List<GameSession> CreateSessions() =>
        [
            CreateSession(DefaultSessionId, DefaultSessionName, DefaultJoinCode),
            CreateSession(2, "Game 2", "JOIN456"),
        ];

    private static User CreateUser(string? id = DefaultUserId, bool isDeleted = false) =>
        new()
        {
            Id = id,
            IsDeleted = isDeleted,
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
    public async ValueTask AddGameSessionAsync_ReturnsNotFound_WhenHostUserNotFound()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns((User?) null);

        OneOf<GameSession, NotFound, DomainError> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            session => Assert.Fail("Expected NotFound but got GameSession"),
            notFound => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask AddGameSessionAsync_ReturnsAddedSession_WhenValid()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        var user = CreateUser();
        var session = CreateSession(10, data.SessionName, data.JoinCode);
        var team = CreateTeam(20, "Team 1", TeamRole.MrX, "#000000");

        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(user);
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns((GameSession?) null);
        _gameSessionRepository.GetSessionByJoinCodeAsync(DefaultJoinCode, false).Returns((GameSession?) null);
        _gameSessionRepository.AddGameSession(DefaultUserId, DefaultSessionName, DefaultJoinCode, 60, 5).Returns(session);
        _teamRepository.AddTeam(session.Id, "Team 1", TeamRole.MrX, "#000000", Team.DefaultMaxPlayerCount).Returns(team);

        OneOf<GameSession, NotFound, DomainError> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            added => added.Should().BeEquivalentTo(session),
            notFound => Assert.Fail("Expected GameSession but got NotFound"),
            domainError => Assert.Fail("Expected GameSession but got DomainError")
        );
        _teamMemberRepository.Received(1).AddTeamMember(session.Id, team.Id, user.Id, null, true);
        await _uow.Received(3).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask AddGameSessionAsync_ReturnsDomainError_WhenHostUserDeleted()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser(DefaultUserId, isDeleted: true));

        OneOf<GameSession, NotFound, DomainError> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got GameSession"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.HostUserDeleted)
        );
    }

    [Fact]
    public async ValueTask AddGameSessionAsync_ReturnsDomainError_WhenHostAlreadyHasActiveSession()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false)
            .Returns(CreateSession(99, "Existing", "EXIST1"));

        OneOf<GameSession, NotFound, DomainError> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got GameSession"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.HostUserAlreadyHasActiveSession)
        );
    }

    [Fact]
    public async ValueTask AddGameSessionAsync_ReturnsDomainError_WhenJoinCodeAlreadyInUse()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, DefaultSessionName, DefaultJoinCode);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns((GameSession?) null);
        _gameSessionRepository.GetSessionByJoinCodeAsync(DefaultJoinCode, false)
            .Returns(CreateSession(2, "Other", DefaultJoinCode));

        OneOf<GameSession, NotFound, DomainError> result = await _sut.AddGameSessionAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got GameSession"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.JoinCodeInUse)
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsNotFound_WhenSessionMissing()
    {
        var data = new IGameSessionService.GameSessionData(DefaultUserId, "Updated", "UPD123");
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsNotFound_WhenHostMissing()
    {
        var existing = CreateSession();
        var data = new IGameSessionService.GameSessionData(DefaultUserId, "Updated", "UPD123");

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns((User?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsDomainError_WhenHostDeleted()
    {
        var existing = CreateSession();
        var data = new IGameSessionService.GameSessionData(DefaultUserId, "Updated", "UPD123");

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser(DefaultUserId, isDeleted: true));

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.HostUserDeleted)
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsDomainError_WhenHostHasDifferentActiveSession()
    {
        var existing = CreateSession();
        var data = new IGameSessionService.GameSessionData(DefaultUserId, "Updated", "UPD123");

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns(CreateSession(99, "Existing", "EXIST1"));

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.HostUserAlreadyHasActiveSession)
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsDomainError_WhenJoinCodeAlreadyInUse()
    {
        var existing = CreateSession();
        var data = new IGameSessionService.GameSessionData(DefaultUserId, "Updated", "UPD123");

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns((GameSession?) null);
        _gameSessionRepository.GetSessionByJoinCodeExcludingIdAsync("UPD123", DefaultSessionId, false).Returns(CreateSession(22, "Other", "UPD123"));

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.JoinCodeInUse)
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsDomainError_WhenStatusTransitionInvalid()
    {
        var existing = CreateSession();
        existing.Status = SessionStatus.Finished;
        var data = new IGameSessionService.GameSessionData(
            DefaultUserId,
            "Updated",
            "UPD123",
            SessionStatus.Active,
            StartTime: null,
            EndTime: null,
            PlannedDurationMinutes: 45,
            MrXRevealInterval: 3);

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns((GameSession?) null);
        _gameSessionRepository.GetSessionByJoinCodeExcludingIdAsync("UPD123", DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.InvalidSessionTransition)
        );
    }

    [Fact]
    public async ValueTask UpdateGameSessionAsync_ReturnsSuccess_WhenValid()
    {
        var existing = CreateSession();
        existing.Status = SessionStatus.Waiting;
        var now = SystemClock.Instance.GetCurrentInstant();
        var data = new IGameSessionService.GameSessionData(
            DefaultUserId,
            "Updated Session",
            "UPD123",
            SessionStatus.Active,
            StartTime: now,
            EndTime: null,
            PlannedDurationMinutes: 45,
            MrXRevealInterval: 4);

        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(CreateUser());
        _gameSessionRepository.GetActiveSessionByHostUserIdAsync(DefaultUserId, false).Returns((GameSession?) null);
        _gameSessionRepository.GetSessionByJoinCodeExcludingIdAsync("UPD123", DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateGameSessionAsync(DefaultSessionId, data, false);

        result.Switch(
            _ => { /* expected */ },
            _ => Assert.Fail("Expected Success but got NotFound"),
            _ => Assert.Fail("Expected Success but got DomainError")
        );
        existing.SessionName.Should().Be("Updated Session");
        existing.JoinCode.Should().Be("UPD123");
        existing.Status.Should().Be(SessionStatus.Active);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask DeleteGameSessionAsync_ReturnsNotFound_WhenUnknown()
    {
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);

        OneOf<Success, NotFound> result = await _sut.DeleteGameSessionAsync(DefaultSessionId, false);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask DeleteGameSessionAsync_ReturnsSuccess_WhenFound()
    {
        var existing = CreateSession();
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(existing);

        OneOf<Success, NotFound> result = await _sut.DeleteGameSessionAsync(DefaultSessionId, false);

        result.Switch(
            _ => { /* expected */ },
            _ => Assert.Fail("Expected Success but got NotFound")
        );
        _gameSessionRepository.Received(1).RemoveSession(existing);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask GetGameSessionByJoinCodeAsync_ReturnsNotFound_WhenUnknown()
    {
        _gameSessionRepository.GetSessionByJoinCodeAsync(DefaultJoinCode, false).Returns((GameSession?) null);

        OneOf<GameSession, NotFound> result = await _sut.GetGameSessionByJoinCodeAsync(DefaultJoinCode, false);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got GameSession"),
            _ => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask GetGameSessionByJoinCodeAsync_ReturnsSession_WhenFound()
    {
        var session = CreateSession();
        _gameSessionRepository.GetSessionByJoinCodeAsync(DefaultJoinCode, false).Returns(session);

        OneOf<GameSession, NotFound> result = await _sut.GetGameSessionByJoinCodeAsync(DefaultJoinCode, false);

        result.Switch(
            found => found.Should().BeEquivalentTo(session),
            _ => Assert.Fail("Expected GameSession but got NotFound")
        );
    }

    // --- StartGameSessionAsync ---

    [Fact]
    public async ValueTask StartGameSessionAsync_ReturnsNotFound_WhenSessionMissing()
    {
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, true).Returns((GameSession?) null);
        OneOf<Success, NotFound, DomainError> result = await _sut.StartGameSessionAsync(DefaultSessionId);
        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask StartGameSessionAsync_ReturnsDomainError_WhenStatusIsNotWaiting()
    {
        var session = CreateSession();
        session.Status = SessionStatus.Active;
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, true).Returns(session);
        OneOf<Success, NotFound, DomainError> result = await _sut.StartGameSessionAsync(DefaultSessionId);
        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.InvalidSessionTransition)
        );
    }

    [Fact]
    public async ValueTask StartGameSessionAsync_ReturnsSuccess_WhenStatusIsWaiting()
    {
        var session = CreateSession();
        session.Status = SessionStatus.Waiting;
        var now = Instant.FromUtc(2026, 3, 15, 12, 0);
        _clock.GetCurrentInstant().Returns(now);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, true).Returns(session);
        OneOf<Success, NotFound, DomainError> result = await _sut.StartGameSessionAsync(DefaultSessionId);
        result.Switch(
            _ => { /* expected */ },
            _ => Assert.Fail("Expected Success but got NotFound"),
            _ => Assert.Fail("Expected Success but got DomainError")
        );
        session.Status.Should().Be(SessionStatus.Active);
        session.StartTime.Should().Be(now);
        await _uow.Received(1).SaveChangesAsync();
    }

    // --- EndGameSessionAsync ---

    [Fact]
    public async ValueTask EndGameSessionAsync_ReturnsNotFound_WhenSessionMissing()
    {
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, true).Returns((GameSession?) null);
        OneOf<Success, NotFound, DomainError> result = await _sut.EndGameSessionAsync(DefaultSessionId);
        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask EndGameSessionAsync_ReturnsDomainError_WhenStatusIsNotActive()
    {
        var session = CreateSession();
        session.Status = SessionStatus.Waiting;
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, true).Returns(session);
        OneOf<Success, NotFound, DomainError> result = await _sut.EndGameSessionAsync(DefaultSessionId);
        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.InvalidSessionTransition)
        );
    }

    [Fact]
    public async ValueTask EndGameSessionAsync_ReturnsSuccess_WhenStatusIsActive()
    {
        var session = CreateSession();
        session.Status = SessionStatus.Active;
        var now = Instant.FromUtc(2026, 3, 15, 14, 0);
        _clock.GetCurrentInstant().Returns(now);
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, true).Returns(session);
        OneOf<Success, NotFound, DomainError> result = await _sut.EndGameSessionAsync(DefaultSessionId);
        result.Switch(
            _ => { /* expected */ },
            _ => Assert.Fail("Expected Success but got NotFound"),
            _ => Assert.Fail("Expected Success but got DomainError")
        );
        session.Status.Should().Be(SessionStatus.Finished);
        session.EndTime.Should().Be(now);
        await _uow.Received(1).SaveChangesAsync();
    }

    // --- CatchMrXAsync ---

    private const int DefaultCatchingTeamId = 20;

    private Team ArrangeActiveCatchScenario(
        out Team mrXTeam,
        TeamRole catchingRole = TeamRole.Detective,
        int catchingTeamSessionId = DefaultSessionId)
    {
        var session = CreateSession();
        session.Status = SessionStatus.Active;
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(session);

        mrXTeam = CreateTeam(10, "MrX Team", TeamRole.MrX, "#000000");
        mrXTeam.SessionId = DefaultSessionId;
        _teamRepository.GetTeamBySessionAndRoleAsync(DefaultSessionId, TeamRole.MrX, true).Returns(mrXTeam);

        var catchingTeam = CreateTeam(DefaultCatchingTeamId, "Detective Team", catchingRole, "#2563EB");
        catchingTeam.SessionId = catchingTeamSessionId;
        _teamRepository.GetTeamByIdAsync(DefaultCatchingTeamId, true).Returns(catchingTeam);

        return catchingTeam;
    }

    [Fact]
    public async ValueTask CatchMrXAsync_ReturnsNotFound_WhenSessionMissing()
    {
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns((GameSession?) null);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            _ => Assert.Fail("Expected NotFound but got MrXCaughtResult"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask CatchMrXAsync_ReturnsDomainError_WhenSessionNotActive()
    {
        var session = CreateSession();
        session.Status = SessionStatus.Waiting;
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(session);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            _ => Assert.Fail("Expected DomainError but got MrXCaughtResult"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotActive)
        );
    }

    [Fact]
    public async ValueTask CatchMrXAsync_ReturnsNotFound_WhenMrXTeamMissing()
    {
        var session = CreateSession();
        session.Status = SessionStatus.Active;
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(session);
        _teamRepository.GetTeamBySessionAndRoleAsync(DefaultSessionId, TeamRole.MrX, true).Returns((Team?) null);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            _ => Assert.Fail("Expected NotFound but got MrXCaughtResult"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask CatchMrXAsync_ReturnsNotFound_WhenCatchingTeamMissing()
    {
        ArrangeActiveCatchScenario(out _);
        _teamRepository.GetTeamByIdAsync(DefaultCatchingTeamId, true).Returns((Team?) null);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            _ => Assert.Fail("Expected NotFound but got MrXCaughtResult"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask CatchMrXAsync_ReturnsDomainError_WhenCatchingTeamNotInSession()
    {
        ArrangeActiveCatchScenario(out _, catchingTeamSessionId: DefaultSessionId + 1);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            _ => Assert.Fail("Expected DomainError but got MrXCaughtResult"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.TeamNotInSession)
        );
    }

    [Fact]
    public async ValueTask CatchMrXAsync_ReturnsDomainError_WhenCatchingTeamNotDetective()
    {
        ArrangeActiveCatchScenario(out _, catchingRole: TeamRole.Spectator);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            _ => Assert.Fail("Expected DomainError but got MrXCaughtResult"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.CatchingTeamNotEligible)
        );
    }

    [Fact]
    public async ValueTask CatchMrXAsync_SwapsRoles_WhenActive()
    {
        var catchingTeam = ArrangeActiveCatchScenario(out var mrXTeam);
        OneOf<IGameSessionService.MrXCaughtResult, NotFound, DomainError> result = await _sut.CatchMrXAsync(DefaultSessionId, DefaultCatchingTeamId);
        result.Switch(
            caught =>
            {
                caught.NewMrXTeam.Should().BeSameAs(catchingTeam);
                caught.FormerMrXTeam.Should().BeSameAs(mrXTeam);
            },
            _ => Assert.Fail("Expected MrXCaughtResult but got NotFound"),
            _ => Assert.Fail("Expected MrXCaughtResult but got DomainError")
        );

        catchingTeam.Role.Should().Be(TeamRole.MrX);
        mrXTeam.Role.Should().Be(TeamRole.Detective);
        catchingTeam.IsCaught.Should().BeFalse();
        mrXTeam.IsCaught.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync();
    }
}
