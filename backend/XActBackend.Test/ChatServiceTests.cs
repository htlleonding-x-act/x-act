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

public sealed class ChatServiceTests
{
    private const int SessionId = 1;
    private const int TeamId = 2;
    private const int OtherTeamId = 3;
    private const int MemberId = 10;
    private const string UserId = "5";

    private static readonly Instant now = Instant.FromUtc(2026, 1, 1, 12, 0);

    private readonly IChatMessageRepository _chatRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _uow;
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _chatRepository = Substitute.For<IChatMessageRepository>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _teamRepository = Substitute.For<ITeamRepository>();
        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _userRepository = Substitute.For<IUserRepository>();

        _uow.ChatMessageRepository.Returns(_chatRepository);
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
        _uow.TeamRepository.Returns(_teamRepository);
        _uow.GameSessionRepository.Returns(_gameSessionRepository);
        _uow.UserRepository.Returns(_userRepository);

        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(now);

        var logger = Substitute.For<ILogger<ChatService>>();
        _sut = new ChatService(_uow, clock, logger);
    }

    private static GameSession CreateSession() =>
        new() { Id = SessionId, HostUserId = "1", SessionName = "S", JoinCode = "ABCDE1", Status = SessionStatus.Active };

    private static Team CreateTeam(int teamId = TeamId, int sessionId = SessionId) =>
        new() { Id = teamId, SessionId = sessionId, TeamName = "T", ColorCode = "#112233", Role = TeamRole.Detective };

    private static TeamMember CreateUserMember(int teamId = TeamId) =>
        new() { Id = MemberId, SessionId = SessionId, TeamId = teamId, UserId = UserId };

    private static TeamMember CreateGuestMember(int teamId = TeamId) =>
        new() { Id = MemberId, SessionId = SessionId, TeamId = teamId, GuestName = "Guest A" };

    private static ChatMessage CreateMessage(int? teamId) =>
        new() { Id = 99, SessionId = SessionId, TeamId = teamId, SenderName = "x", Content = "hi" };

    [Fact]
    public async ValueTask GetSessionMessagesAsync_ReturnsMessages_WhenSessionExists()
    {
        var messages = new List<ChatMessage> { CreateMessage(null) };
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _chatRepository.GetSessionMessagesAsync(SessionId, Arg.Any<int>(), false).Returns(messages);

        OneOf<IReadOnlyCollection<ChatMessage>, NotFound> result = await _sut.GetSessionMessagesAsync(SessionId, 50);

        result.Switch(
            found => found.Should().BeEquivalentTo(messages),
            _ => Assert.Fail("Expected messages but got NotFound"));
    }

    [Fact]
    public async ValueTask GetSessionMessagesAsync_ReturnsNotFound_WhenSessionMissing()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns((GameSession?)null);

        OneOf<IReadOnlyCollection<ChatMessage>, NotFound> result = await _sut.GetSessionMessagesAsync(SessionId, 50);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got messages"),
            _ => { /* expected */ });
    }

    [Fact]
    public async ValueTask GetTeamMessagesAsync_ReturnsMessages_WhenTeamInSession()
    {
        var messages = new List<ChatMessage> { CreateMessage(TeamId) };
        _teamRepository.GetTeamByIdAsync(TeamId, false).Returns(CreateTeam());
        _chatRepository.GetTeamMessagesAsync(SessionId, TeamId, Arg.Any<int>(), false).Returns(messages);

        OneOf<IReadOnlyCollection<ChatMessage>, NotFound> result = await _sut.GetTeamMessagesAsync(SessionId, TeamId, 50);

        result.Switch(
            found => found.Should().BeEquivalentTo(messages),
            _ => Assert.Fail("Expected messages but got NotFound"));
    }

    [Fact]
    public async ValueTask GetTeamMessagesAsync_ReturnsNotFound_WhenTeamNotInSession()
    {
        _teamRepository.GetTeamByIdAsync(TeamId, false).Returns(CreateTeam(TeamId, sessionId: 999));

        OneOf<IReadOnlyCollection<ChatMessage>, NotFound> result = await _sut.GetTeamMessagesAsync(SessionId, TeamId, 50);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got messages"),
            _ => { /* expected */ });
    }

    [Fact]
    public async ValueTask PostSessionMessageAsync_PersistsAndResolvesUsername()
    {
        var message = CreateMessage(null);
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(MemberId, false).Returns(CreateUserMember());
        _userRepository.GetUserByIdAsync(UserId, false).Returns(new User { Id = UserId, Username = "detective_user" });
        _chatRepository.AddChatMessage(SessionId, null, MemberId, TeamId, "detective_user", "Hello", now).Returns(message);

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostSessionMessageAsync(SessionId, MemberId, "  Hello  ");

        result.Switch(
            created => created.Should().BeSameAs(message),
            _ => Assert.Fail("Expected message but got NotFound"),
            _ => Assert.Fail("Expected message but got DomainError"));

        _chatRepository.Received(1).AddChatMessage(SessionId, null, MemberId, TeamId, "detective_user", "Hello", now);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask PostSessionMessageAsync_UsesGuestName_WhenNoUser()
    {
        var message = CreateMessage(null);
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(MemberId, false).Returns(CreateGuestMember());
        _chatRepository.AddChatMessage(SessionId, null, MemberId, TeamId, "Guest A", "Hi", now).Returns(message);

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostSessionMessageAsync(SessionId, MemberId, "Hi");

        result.IsT0.Should().BeTrue();
        _chatRepository.Received(1).AddChatMessage(SessionId, null, MemberId, TeamId, "Guest A", "Hi", now);
    }

    [Fact]
    public async ValueTask PostSessionMessageAsync_ReturnsNotFound_WhenSessionMissing()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns((GameSession?)null);

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostSessionMessageAsync(SessionId, MemberId, "Hi");

        result.IsT1.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async ValueTask PostSessionMessageAsync_ReturnsNotFound_WhenMemberNotInSession()
    {
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(MemberId, false).Returns(new TeamMember { Id = MemberId, SessionId = 999, TeamId = TeamId });

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostSessionMessageAsync(SessionId, MemberId, "Hi");

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async ValueTask PostTeamMessageAsync_PersistsForOwnTeam()
    {
        var message = CreateMessage(TeamId);
        _teamRepository.GetTeamByIdAsync(TeamId, false).Returns(CreateTeam());
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(MemberId, false).Returns(CreateGuestMember());
        _chatRepository.AddChatMessage(SessionId, TeamId, MemberId, TeamId, "Guest A", "Team msg", now).Returns(message);

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostTeamMessageAsync(SessionId, TeamId, MemberId, "Team msg");

        result.IsT0.Should().BeTrue();
        _chatRepository.Received(1).AddChatMessage(SessionId, TeamId, MemberId, TeamId, "Guest A", "Team msg", now);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask PostTeamMessageAsync_ReturnsDomainError_WhenSenderNotOnTeam()
    {
        _teamRepository.GetTeamByIdAsync(TeamId, false).Returns(CreateTeam());
        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(CreateSession());
        _teamMemberRepository.GetMemberByIdAsync(MemberId, false).Returns(CreateGuestMember(OtherTeamId));

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostTeamMessageAsync(SessionId, TeamId, MemberId, "Team msg");

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got message"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.ChatNotTeamMember));
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async ValueTask PostTeamMessageAsync_ReturnsNotFound_WhenTeamNotInSession()
    {
        _teamRepository.GetTeamByIdAsync(TeamId, false).Returns(CreateTeam(TeamId, sessionId: 999));

        OneOf<ChatMessage, NotFound, DomainError> result = await _sut.PostTeamMessageAsync(SessionId, TeamId, MemberId, "Team msg");

        result.IsT1.Should().BeTrue();
    }
}
