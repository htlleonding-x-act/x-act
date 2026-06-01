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

public sealed class LocationLogServiceTests
{
    private const int DefaultLogId = 1;
    private const int DefaultMemberId = 1;
    private const int DefaultSessionId = 1;
    private const int DefaultTeamId = 1;

    private readonly ILocationLogRepository _locationLogRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly LocationLogService _sut;
    private readonly IUnitOfWork _uow;

    public LocationLogServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _locationLogRepository = Substitute.For<ILocationLogRepository>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _uow.LocationLogRepository.Returns(_locationLogRepository);
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
        _uow.GameSessionRepository.Returns(_gameSessionRepository);
        var logger = Substitute.For<ILogger<LocationLogService>>();
        _sut = new LocationLogService(_uow, logger);
    }

    private static TeamMember CreateMember() =>
        new()
        {
            Id = DefaultMemberId,
            SessionId = DefaultSessionId,
            TeamId = DefaultTeamId,
        };

    private static GameSession CreateActiveSession() =>
        new()
        {
            Id = DefaultSessionId,
            HostUserId = "1",
            SessionName = "Active Session",
            JoinCode = "ACTIV1",
            Status = SessionStatus.Active,
        };

    private static GameSession CreateWaitingSession() =>
        new()
        {
            Id = DefaultSessionId,
            HostUserId = "1",
            SessionName = "Waiting Session",
            JoinCode = "WAIT01",
            Status = SessionStatus.Waiting,
        };

    private static LocationLog CreateLog(int id = DefaultLogId, int memberId = DefaultMemberId) =>
        new()
        {
            Id = id,
            MemberId = memberId,
        };

    private static List<LocationLog> CreateLogs(int memberId = DefaultMemberId) =>
        [
            CreateLog(DefaultLogId, memberId),
            CreateLog(2, memberId),
        ];

    [Fact]
    public async ValueTask GetLogsByMemberIdAsync_ReturnsLogs()
    {
        var logs = CreateLogs();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _locationLogRepository.GetLogsByMemberIdAsync(DefaultMemberId, false).Returns(logs);

        var result = await _sut.GetLogsByMemberIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false);

        result.Should().BeEquivalentTo(logs);
    }

    [Fact]
    public async ValueTask GetLogsByMemberIdAsync_ReturnsEmpty_WhenMemberMissing()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns((TeamMember?) null);

        var result = await _sut.GetLogsByMemberIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false);

        result.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetLogsBySessionIdAsync_ReturnsLogs()
    {
        var logs = new List<LocationLog> { CreateLog(), CreateLog(2, DefaultMemberId) };
        _locationLogRepository.GetLogsBySessionIdAsync(DefaultSessionId, false).Returns(logs);

        var result = await _sut.GetLogsBySessionIdAsync(DefaultSessionId, false);

        result.Should().BeEquivalentTo(logs);
    }

    [Fact]
    public async ValueTask GetLocationLogByIdAsync_ReturnsLog_WhenFound()
    {
        var log = CreateLog();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _locationLogRepository.GetLogByMemberAndIdAsync(DefaultMemberId, DefaultLogId, false).Returns(log);

        OneOf<LocationLog, NotFound> result = await _sut.GetLocationLogByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, false);

        result.Switch(
            found => found.Should().BeEquivalentTo(log),
            notFound => Assert.Fail("Expected LocationLog but got NotFound")
        );
    }

    [Fact]
    public async ValueTask GetLocationLogByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _locationLogRepository.GetLogByMemberAndIdAsync(DefaultMemberId, DefaultLogId, false).Returns((LocationLog?) null);

        OneOf<LocationLog, NotFound> result = await _sut.GetLocationLogByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, false);

        result.Switch(
            log => Assert.Fail("Expected NotFound but got LocationLog"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask GetLocationLogByIdAsync_ReturnsNotFound_WhenMemberMissing()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns((TeamMember?) null);

        OneOf<LocationLog, NotFound> result = await _sut.GetLocationLogByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, false);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got LocationLog"),
            _ => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask AddLocationLogAsync_ReturnsAddedLog()
    {
        var timestamp = SystemClock.Instance.GetCurrentInstant();
        var data = new ILocationLogService.LocationLogData(DefaultMemberId, timestamp, 10.0, 20.0, 5.0, TransportMode.Foot, false);
        var log = new LocationLog { Id = DefaultLogId, MemberId = data.MemberId, Timestamp = data.Timestamp };

        _locationLogRepository.AddLocationLog(
            data.MemberId,
            data.Timestamp,
            data.Latitude,
            data.Longitude,
            data.AccuracyMeters,
            data.TransportMode,
            data.IsRevealedPosition
        ).Returns(log);

        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns(CreateMember());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());

        OneOf<LocationLog, NotFound, DomainError> result = await _sut.AddLocationLogAsync(data);

        result.Switch(
            addedLog => addedLog.Should().BeEquivalentTo(log),
            notFound => Assert.Fail("Expected LocationLog but got NotFound"),
            domainError => Assert.Fail("Expected LocationLog but got DomainError")
        );
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask AddLocationLogAsync_ReturnsNotFound_WhenMemberMissing()
    {
        var data = new ILocationLogService.LocationLogData(DefaultMemberId, SystemClock.Instance.GetCurrentInstant(), 10.0, 20.0, 5.0, TransportMode.Foot, false);
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns((TeamMember?) null);

        OneOf<LocationLog, NotFound, DomainError> result = await _sut.AddLocationLogAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got LocationLog"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask AddLocationLogAsync_ReturnsDomainError_WhenSessionNotActive()
    {
        var data = new ILocationLogService.LocationLogData(DefaultMemberId, SystemClock.Instance.GetCurrentInstant(), 10.0, 20.0, 5.0, TransportMode.Foot, false);
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns(CreateMember());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());

        OneOf<LocationLog, NotFound, DomainError> result = await _sut.AddLocationLogAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got LocationLog"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotActive)
        );
    }

    [Fact]
    public async ValueTask UpdateLocationLogAsync_ReturnsSuccess_WhenFound()
    {
        var log = CreateLog();
        var data = new ILocationLogService.LocationLogData(DefaultMemberId, SystemClock.Instance.GetCurrentInstant(), 10.0, 20.0, 5.0, TransportMode.Foot, true);

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _locationLogRepository.GetLogByMemberAndIdAsync(DefaultMemberId, DefaultLogId, true).Returns(log);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateLocationLogAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail("Expected Success but got DomainError")
        );
        log.Latitude.Should().Be(data.Latitude);
        log.IsRevealedPosition.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateLocationLogAsync_ReturnsNotFound_WhenUnknown()
    {
        var data = new ILocationLogService.LocationLogData(DefaultMemberId, SystemClock.Instance.GetCurrentInstant(), 10.0, 20.0, 5.0, TransportMode.Foot, true);

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _locationLogRepository.GetLogByMemberAndIdAsync(DefaultMemberId, DefaultLogId, true).Returns((LocationLog?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateLocationLogAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask UpdateLocationLogAsync_ReturnsNotFound_WhenPayloadMemberDoesNotMatch()
    {
        var data = new ILocationLogService.LocationLogData(999, SystemClock.Instance.GetCurrentInstant(), 10.0, 20.0, 5.0, TransportMode.Foot, true);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateLocationLogAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    public async ValueTask UpdateLocationLogAsync_ReturnsDomainError_WhenSessionNotActive()
    {
        var data = new ILocationLogService.LocationLogData(DefaultMemberId, SystemClock.Instance.GetCurrentInstant(), 10.0, 20.0, 5.0, TransportMode.Foot, true);
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdateLocationLogAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotActive)
        );
    }

    [Fact]
    public async ValueTask DeleteLocationLogAsync_ReturnsSuccess_WhenFound()
    {
        var log = CreateLog();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _locationLogRepository.GetLogByMemberAndIdAsync(DefaultMemberId, DefaultLogId, true).Returns(log);

        OneOf<Success, NotFound> result = await _sut.DeleteLocationLogAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        _locationLogRepository.Received(1).RemoveLocationLog(log);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask DeleteLocationLogAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _locationLogRepository.GetLogByMemberAndIdAsync(DefaultMemberId, DefaultLogId, true).Returns((LocationLog?) null);

        OneOf<Success, NotFound> result = await _sut.DeleteLocationLogAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultLogId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }
}
