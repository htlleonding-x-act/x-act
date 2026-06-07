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

    private readonly IUserRepository _userRepository;
    private readonly UserService _sut;
    private readonly IUnitOfWork _uow;

    public UserServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _userRepository = Substitute.For<IUserRepository>();
        _uow.UserRepository.Returns(_userRepository);
        var logger = Substitute.For<ILogger<UserService>>();
        _sut = new UserService(_uow, logger);
    }

    private static User CreateUser(
        string? id = DefaultUserId,
        string? username = null,
        string? email = null
    ) =>
        new()
        {
            Id = id,
            Username = username ?? DefaultUsername,
            Email = email ?? DefaultEmail,
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
    public async ValueTask GetUserByUsernameAsync_ReturnsUser_WhenFound()
    {
        var user = CreateUser(DefaultUserId, "test", DefaultEmail);
        _userRepository.GetUserByUsernameAsync("test", false).Returns(user);

        OneOf<User, NotFound> result = await _sut.GetUserByUsernameAsync("test", false);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
            notFound => Assert.Fail("Expected a user but got NotFound")
        );
    }

    [Fact]
    public async ValueTask GetUserByUsernameAsync_ReturnsNotFound_WhenUnknown()
    {
        _userRepository.GetUserByUsernameAsync("test", false).Returns((User?) null);

        OneOf<User, NotFound> result = await _sut.GetUserByUsernameAsync("test", false);

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

        _userRepository.AddUser(data.Username, data.Email, data.AccountType).Returns(user);

        OneOf<User, Error> result = await _sut.AddUserAsync(data);

        result.Switch(
            found => found.Should().BeEquivalentTo(user),
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
