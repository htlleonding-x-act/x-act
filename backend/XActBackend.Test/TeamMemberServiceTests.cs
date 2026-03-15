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

public sealed class TeamMemberServiceTests
{
    private const int DefaultSessionId = 1;
    private const int DefaultTeamId = 1;
    private const int DefaultMemberId = 1;
    private const int DefaultUserId = 10;

    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly TeamMemberService _sut;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public TeamMemberServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);

        _clock = Substitute.For<IClock>();
        _sut = new TeamMemberService(_uow, _clock);
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
    internal async ValueTask AddTeamMemberAsync_ReturnsError_WhenNeitherUserIdNorGuestNameProvided()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, null, null);

        OneOf<TeamMember, Error> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            teamMember => Assert.Fail("Expected Error but got TeamMember"),
            error => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsError_WhenUserAlreadyInSession()
    {
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null);
        var existingMember = CreateMember(2, DefaultSessionId, DefaultTeamId, DefaultUserId);

        _teamMemberRepository.GetMemberBySessionAndUserIdAsync(DefaultSessionId, DefaultUserId, false).Returns(existingMember);

        OneOf<TeamMember, Error> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            teamMember => Assert.Fail("Expected Error but got TeamMember"),
            error => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask AddTeamMemberAsync_ReturnsAddedMember()
    {
        var lastUpdated = SystemClock.Instance.GetCurrentInstant();
        var data = new ITeamMemberService.TeamMemberData(DefaultSessionId, DefaultTeamId, DefaultUserId, null, true, 45.0, 90.0, lastUpdated);
        var member = CreateMember(DefaultMemberId, DefaultSessionId, DefaultTeamId, DefaultUserId);
        member.IsTeamLeader = true;

        _teamMemberRepository.GetMemberBySessionAndUserIdAsync(DefaultSessionId, DefaultUserId, false).Returns((TeamMember?) null);
        _teamMemberRepository.AddTeamMember(DefaultSessionId, DefaultTeamId, DefaultUserId, null, true).Returns(member);

        OneOf<TeamMember, Error> result = await _sut.AddTeamMemberAsync(data);

        result.Switch(
            teamMember => teamMember.Should().BeEquivalentTo(member),
            error => Assert.Fail("Expected TeamMember but got Error")
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

        OneOf<Success, NotFound> result = await _sut.UpdateTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
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

        OneOf<Success, NotFound> result = await _sut.UpdateTeamMemberAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        member.GuestName.Should().Be("New Guest");
        member.LastUpdated.Should().Be(now);
        await _uow.Received(1).SaveChangesAsync();
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
