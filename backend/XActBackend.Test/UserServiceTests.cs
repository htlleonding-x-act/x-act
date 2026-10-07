using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf;
using OneOf.Types;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Repositories;
using XActBackend.Persistence.Util;

namespace XActBackend.Test;

public sealed class UserServiceTests
{
    private const string DefaultUserId = "1";
    private const string DefaultUsername = "user1";
    private const string DefaultEmail = "user1@test.com";
    private const string KeycloakSubject = "4b9c1f4e-0a55-4e3a-9d0b-6c2f0c6b1a77";

    private readonly IUserAuthIdentityRepository _authIdentityRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserService _sut;
    private readonly IUnitOfWork _uow;

    public UserServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _userRepository = Substitute.For<IUserRepository>();
        _uow.UserRepository.Returns(_userRepository);
        _authIdentityRepository = Substitute.For<IUserAuthIdentityRepository>();
        _uow.UserAuthIdentityRepository.Returns(_authIdentityRepository);
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Instant.FromUtc(2026, 1, 1, 12, 0));
        var logger = Substitute.For<ILogger<UserService>>();
        _sut = new UserService(_uow, clock, logger);
    }

    private static User CreateUser(
        string id = DefaultUserId,
        string? username = null,
        string? email = null
    ) =>
        new()
        {
            Id = id,
            Username = username ?? DefaultUsername,
            Email = email ?? DefaultEmail,
        };

    private static UserAuthIdentity CreateAuthIdentity(string userId) =>
        new()
        {
            UserId = userId,
            ProviderSubject = KeycloakSubject,
        };

    private static List<User> CreateUsers() =>
        [
            CreateUser(DefaultUserId, DefaultUsername, DefaultEmail),
            CreateUser("2", "user2", "user2@test.com"),
        ];

    [Fact]
    public async ValueTask GetAllUsersAsync_ReturnsUsers()
    {
        var users = CreateUsers();
        _userRepository.GetAllUsersAsync(false).Returns(users);

        var result = await _sut.GetAllUsersAsync(false);

        result.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async ValueTask GetUserByIdAsync_ReturnsUser_WhenFound()
    {
        var user = CreateUser(DefaultUserId, "user1", DefaultEmail);
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns(user);

        OneOf<User, NotFound> result = await _sut.GetUserByIdAsync(DefaultUserId, false);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
            notFound => Assert.Fail("Expected a user but got NotFound")
        );
    }

    [Fact]
    public async ValueTask GetUserByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _userRepository.GetUserByIdAsync(DefaultUserId, false).Returns((User?) null);

        OneOf<User, NotFound> result = await _sut.GetUserByIdAsync(DefaultUserId, false);

        result.Switch(
            user => Assert.Fail("Expected NotFound but got a user"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask GetUserByEmailAsync_ReturnsUser_WhenFound()
    {
        var user = CreateUser(DefaultUserId, DefaultUsername, "test@test.com");
        _userRepository.GetUserByEmailAsync("test@test.com", false).Returns(user);

        OneOf<User, NotFound> result = await _sut.GetUserByEmailAsync("test@test.com", false);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
            notFound => Assert.Fail("Expected a user but got NotFound")
        );
    }

    [Fact]
    public async ValueTask GetUserByEmailAsync_ReturnsNotFound_WhenUnknown()
    {
        _userRepository.GetUserByEmailAsync("test@test.com", false).Returns((User?) null);

        OneOf<User, NotFound> result = await _sut.GetUserByEmailAsync("test@test.com", false);

        result.Switch(
            user => Assert.Fail("Expected NotFound but got a user"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask AddUserAsync_ReturnsAddedUser()
    {
        var data = new IUserService.UserData("new_user", "new@test.com");
        var user = CreateUser(DefaultUserId, data.Username, data.Email);

        _userRepository.GetRegisteredUserByUsernameAsync(data.Username, false).Returns((User?) null);
        _userRepository.AddUser(data.Username, data.Email, data.AccountType, true).Returns(user);

        OneOf<User, DomainError, Error> result = await _sut.AddUserAsync(data);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
            domainError => Assert.Fail("Expected a user but got a DomainError"),
            error => Assert.Fail("Expected a user but got an Error")
        );
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask AddUserAsync_ReturnsDomainError_WhenUsernameTaken()
    {
        var data = new IUserService.UserData(DefaultUsername, "other@test.com");
        _userRepository.GetRegisteredUserByUsernameAsync(DefaultUsername, false).Returns(CreateUser());

        OneOf<User, DomainError, Error> result = await _sut.AddUserAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected a DomainError but got a user"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.UsernameTaken),
            _ => Assert.Fail("Expected a DomainError but got an Error")
        );
        _userRepository.DidNotReceiveWithAnyArgs().AddUser(default!, default!, default, default, default);
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async ValueTask AddUserAsync_CreatesGuest_WhenOnlyAnotherGuestHasTheName()
    {
        var data = new IUserService.UserData(DefaultUsername, null);
        var user = CreateUser(DefaultUserId, data.Username);
        _userRepository.GetRegisteredUserByUsernameAsync(DefaultUsername, false).Returns((User?) null);
        _userRepository.AddUser(DefaultUsername, null, AccountType.Free, true).Returns(user);

        OneOf<User, DomainError, Error> result = await _sut.AddUserAsync(data);

        result.Switch(
            created => created.Should().BeEquivalentTo(user),
            _ => Assert.Fail("Expected a user but got a DomainError"),
            _ => Assert.Fail("Expected a user but got an Error")
        );
        _userRepository.Received(1).AddUser(DefaultUsername, null, AccountType.Free, true);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask GetOrCreateByKeycloakSubjectAsync_ReturnsLinkedUser_WhenIdentityExists()
    {
        var user = CreateUser(KeycloakSubject, DefaultUsername, DefaultEmail);
        _authIdentityRepository.GetBySubjectAsync(KeycloakSubject, false)
                               .Returns(CreateAuthIdentity(KeycloakSubject));
        _userRepository.GetUserByIdAsync(KeycloakSubject, true).Returns(user);

        OneOf<User, Error> result =
            await _sut.GetOrCreateByKeycloakSubjectAsync(KeycloakSubject, DefaultUsername, DefaultEmail);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
            error => Assert.Fail("Expected a user but got an Error")
        );
        _userRepository.DidNotReceiveWithAnyArgs().AddUser(default!, default!, default, default, default);
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async ValueTask GetOrCreateByKeycloakSubjectAsync_CreatesUserAndIdentity_OnFirstLogin()
    {
        var user = CreateUser(KeycloakSubject, DefaultUsername, DefaultEmail);
        _authIdentityRepository.GetBySubjectAsync(KeycloakSubject, false).Returns((UserAuthIdentity?) null);
        _userRepository.AddUser(DefaultUsername, DefaultEmail, AccountType.Free, false, KeycloakSubject).Returns(user);

        OneOf<User, Error> result =
            await _sut.GetOrCreateByKeycloakSubjectAsync(KeycloakSubject, DefaultUsername, DefaultEmail);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
            error => Assert.Fail("Expected a user but got an Error")
        );
        _authIdentityRepository.Received(1).AddAuthIdentity(KeycloakSubject, KeycloakSubject);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask GetOrCreateByKeycloakSubjectAsync_ReturnsError_WhenLinkedUserIsGone()
    {
        _authIdentityRepository.GetBySubjectAsync(KeycloakSubject, false)
                               .Returns(CreateAuthIdentity(DefaultUserId));
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns((User?) null);

        OneOf<User, Error> result =
            await _sut.GetOrCreateByKeycloakSubjectAsync(KeycloakSubject, DefaultUsername, DefaultEmail);

        result.Switch(
            user => Assert.Fail("Expected an Error but got a user"),
            error => { /* expected */ }
        );
        _userRepository.DidNotReceiveWithAnyArgs().AddUser(default!, default!, default, default, default);
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async ValueTask GetOrCreateByKeycloakSubjectAsync_StoresKeycloakUsernameUnchanged()
    {
        string longUsername = new('a', 60);

        await _sut.GetOrCreateByKeycloakSubjectAsync(KeycloakSubject, longUsername, DefaultEmail);

        _userRepository.Received(1).AddUser(longUsername, DefaultEmail, AccountType.Free, false, KeycloakSubject);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(DefaultEmail)]
    public async ValueTask GetOrCreateByKeycloakSubjectAsync_DropsEmail_WhenMissingOrTaken(string? email)
    {
        _userRepository.GetUserByEmailAsync(DefaultEmail, false).Returns(CreateUser());

        await _sut.GetOrCreateByKeycloakSubjectAsync(KeycloakSubject, DefaultUsername, email);

        _userRepository.Received(1).AddUser(DefaultUsername, null, AccountType.Free, false, KeycloakSubject);
    }

    [Fact]
    public async ValueTask GetOrCreateByKeycloakSubjectAsync_RestoresDeletedUser()
    {
        var user = CreateUser(KeycloakSubject, $"deleted_user_{KeycloakSubject}", $"deleted_user_{KeycloakSubject}@deleted.local");
        user.IsDeleted = true;
        user.DeletedAt = Instant.FromUtc(2026, 1, 1, 10, 0);
        _authIdentityRepository.GetBySubjectAsync(KeycloakSubject, false)
                               .Returns(CreateAuthIdentity(KeycloakSubject));
        _userRepository.GetUserByIdAsync(KeycloakSubject, true).Returns(user);

        OneOf<User, Error> result =
            await _sut.GetOrCreateByKeycloakSubjectAsync(KeycloakSubject, DefaultUsername, DefaultEmail);

        result.Switch(
            restored =>
            {
                restored.IsDeleted.Should().BeFalse();
                restored.DeletedAt.Should().BeNull();
                restored.Username.Should().Be(DefaultUsername);
                restored.Email.Should().Be(DefaultEmail);
            },
            error => Assert.Fail("Expected a user but got an Error")
        );
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateUserAsync_ReturnsSuccess_WhenFound()
    {
        var user = CreateUser(DefaultUserId, "old", "old@test.com");
        var data = new IUserService.UserData("new", "new@test.com");
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns(user);

        OneOf<Success, NotFound> result = await _sut.UpdateUserAsync(DefaultUserId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        user.Username.Should().Be(data.Username);
        user.Email.Should().Be(data.Email);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateUserAsync_ReturnsNotFound_WhenUnknown()
    {
        var data = new IUserService.UserData("new", "new@test.com");
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns((User?) null);

        OneOf<Success, NotFound> result = await _sut.UpdateUserAsync(DefaultUserId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask UpdateProfileAsync_RenamesUser_WhenNameIsFree()
    {
        var user = CreateUser(DefaultUserId, "old");
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns(user);
        _userRepository.GetRegisteredUserByUsernameAsync("new", false).Returns((User?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateProfileAsync(DefaultUserId, "new");

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail($"Expected Success but got {domainError.Code}")
        );
        user.Username.Should().Be("new");
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateProfileAsync_RenamesUser_WhenOnlyTheCaseOfTheOwnNameChanges()
    {
        var user = CreateUser(DefaultUserId, "player");
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns(user);
        _userRepository.GetRegisteredUserByUsernameAsync("Player", false).Returns(user);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateProfileAsync(DefaultUserId, "Player");

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail($"Expected Success but got {domainError.Code}")
        );
        user.Username.Should().Be("Player");
    }

    [Fact]
    public async ValueTask UpdateProfileAsync_ReturnsDomainError_WhenAnotherUserHasTheName()
    {
        var user = CreateUser(DefaultUserId, "old");
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns(user);
        _userRepository.GetRegisteredUserByUsernameAsync("taken", false).Returns(CreateUser("2", "taken"));

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateProfileAsync(DefaultUserId, "taken");

        result.Switch(
            success => Assert.Fail("Expected DomainError but got Success"),
            notFound => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.UsernameTaken)
        );
        user.Username.Should().Be("old");
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateProfileAsync_ReturnsNotFound_WhenUnknown()
    {
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns((User?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateProfileAsync(DefaultUserId, "new");

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ },
            domainError => Assert.Fail($"Expected NotFound but got {domainError.Code}")
        );
    }

    [Fact]
    public async ValueTask DeleteUserAsync_ReturnsSuccess_WhenFound()
    {
        var user = CreateUser(DefaultUserId, "user", "user@test.com");
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns(user);

        OneOf<Success, NotFound> result = await _sut.DeleteUserAsync(DefaultUserId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        user.IsDeleted.Should().BeTrue();
        user.Username.Should().Be("deleted_user_1");
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask DeleteUserAsync_ReturnsNotFound_WhenUnknown()
    {
        _userRepository.GetUserByIdAsync(DefaultUserId, true).Returns((User?) null);

        OneOf<Success, NotFound> result = await _sut.DeleteUserAsync(DefaultUserId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }
}
