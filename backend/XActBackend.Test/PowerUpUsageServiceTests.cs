using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Repositories;
using XActBackend.Persistence.Util;
using Xunit;

namespace XActBackend.Test;

public sealed class PowerUpUsageServiceTests
{
    private const int DefaultUsageId = 1;
    private const int DefaultMemberId = 1;
    private const int DefaultSessionId = 1;
    private const int DefaultTeamId = 1;

    private readonly IPowerUpUsageRepository _powerUpUsageRepository;
    private readonly ITeamMemberRepository _teamMemberRepository;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly PowerUpUsageService _sut;
    private readonly IUnitOfWork _uow;

    public PowerUpUsageServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _powerUpUsageRepository = Substitute.For<IPowerUpUsageRepository>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _gameSessionRepository = Substitute.For<IGameSessionRepository>();
        _teamRepository = Substitute.For<ITeamRepository>();
        _uow.PowerUpUsageRepository.Returns(_powerUpUsageRepository);
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
        _uow.GameSessionRepository.Returns(_gameSessionRepository);
        _uow.TeamRepository.Returns(_teamRepository);
        var logger = Substitute.For<ILogger<PowerUpUsageService>>();
        _sut = new PowerUpUsageService(_uow, logger);
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
            SessionName = "Active Session",
            JoinCode = "ACTIV1",
            Status = SessionStatus.Active,
        };

    private static GameSession CreateWaitingSession() =>
        new()
        {
            Id = DefaultSessionId,
            SessionName = "Waiting Session",
            JoinCode = "WAIT01",
            Status = SessionStatus.Waiting,
        };

    private static Team CreateMrXTeam() =>
        new()
        {
            Id = DefaultTeamId,
            SessionId = DefaultSessionId,
            TeamName = "MrX",
            Role = TeamRole.MrX,
            ColorCode = "#000000",
        };

    private static Team CreateDetectiveTeam() =>
        new()
        {
            Id = DefaultTeamId,
            SessionId = DefaultSessionId,
            TeamName = "Detectives",
            Role = TeamRole.Detective,
            ColorCode = "#ff0000",
        };

    private static PowerUpUsage CreateUsage(
        int id = DefaultUsageId,
        int memberId = DefaultMemberId,
        PowerUpType powerUpType = PowerUpType.DoubleMove,
        Instant? usedAt = null
    ) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            PowerUpType = powerUpType,
            UsedAt = usedAt ?? SystemClock.Instance.GetCurrentInstant(),
        };

    private static List<PowerUpUsage> CreateUsages() =>
        [
            CreateUsage(DefaultUsageId, DefaultMemberId, PowerUpType.DoubleMove),
            CreateUsage(2, DefaultMemberId, PowerUpType.BlackTicket),
        ];

    [Fact]
    internal async ValueTask GetUsagesByMemberIdAsync_ReturnsUsages()
    {
        var usages = CreateUsages();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, false).Returns(usages);

        var result = await _sut.GetUsagesByMemberIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false);

        result.Should().BeEquivalentTo(usages);
    }

    [Fact]
    internal async ValueTask GetUsagesByMemberIdAsync_ReturnsEmpty_WhenMemberMissing()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns((TeamMember?) null);

        var result = await _sut.GetUsagesByMemberIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false);

        result.Should().BeEmpty();
    }

    [Fact]
    internal async ValueTask GetPowerUpUsageByIdAsync_ReturnsUsage_WhenFound()
    {
        var expectedUsage = CreateUsage();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, false).Returns(expectedUsage);

        OneOf<PowerUpUsage, NotFound> result = await _sut.GetPowerUpUsageByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, false);

        result.Switch(
            usage => usage.Should().BeEquivalentTo(expectedUsage),
            notFound => Assert.Fail("Expected PowerUpUsage but got NotFound")
        );
    }

    [Fact]
    internal async ValueTask GetPowerUpUsageByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, false).Returns((PowerUpUsage?) null);

        OneOf<PowerUpUsage, NotFound> result = await _sut.GetPowerUpUsageByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, false);

        result.Switch(
            usage => Assert.Fail("Expected NotFound but got PowerUpUsage"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask GetPowerUpUsageByIdAsync_ReturnsNotFound_WhenMemberMissing()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns((TeamMember?) null);

        OneOf<PowerUpUsage, NotFound> result = await _sut.GetPowerUpUsageByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, false);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got PowerUpUsage"),
            _ => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask AddPowerUpUsageAsync_ReturnsAddedUsage()
    {
        var usedAt = SystemClock.Instance.GetCurrentInstant();
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, usedAt);
        var usage = new PowerUpUsage { Id = DefaultUsageId, MemberId = data.MemberId, PowerUpType = data.PowerUpType, UsedAt = data.UsedAt };

        _powerUpUsageRepository.AddPowerUpUsage(data.MemberId, data.PowerUpType, data.UsedAt).Returns(usage);
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns(CreateMember());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateMrXTeam());

        OneOf<PowerUpUsage, NotFound, DomainError> result = await _sut.AddPowerUpUsageAsync(data);

        result.Switch(
            addedUsage => addedUsage.Should().BeEquivalentTo(usage),
            notFound => Assert.Fail("Expected PowerUpUsage but got NotFound"),
            domainError => Assert.Fail("Expected PowerUpUsage but got DomainError")
        );
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask AddPowerUpUsageAsync_ReturnsNotFound_WhenMemberMissing()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns((TeamMember?) null);

        OneOf<PowerUpUsage, NotFound, DomainError> result = await _sut.AddPowerUpUsageAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got PowerUpUsage"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask AddPowerUpUsageAsync_ReturnsDomainError_WhenSessionNotActive()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns(CreateMember());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateWaitingSession());

        OneOf<PowerUpUsage, NotFound, DomainError> result = await _sut.AddPowerUpUsageAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got PowerUpUsage"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.SessionNotActive)
        );
    }

    [Fact]
    internal async ValueTask AddPowerUpUsageAsync_ReturnsNotFound_WhenTeamMissing()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns(CreateMember());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns((Team?) null);

        OneOf<PowerUpUsage, NotFound, DomainError> result = await _sut.AddPowerUpUsageAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got PowerUpUsage"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask AddPowerUpUsageAsync_ReturnsDomainError_WhenTeamRoleIsNotMrX()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());
        _teamMemberRepository.GetMemberByIdAsync(DefaultMemberId, false).Returns(CreateMember());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateDetectiveTeam());

        OneOf<PowerUpUsage, NotFound, DomainError> result = await _sut.AddPowerUpUsageAsync(data);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got PowerUpUsage"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.PowerUpNotAllowedForTeamRole)
        );
    }

    [Fact]
    internal async ValueTask UpdatePowerUpUsageAsync_ReturnsSuccess_WhenFound()
    {
        var usage = CreateUsage(DefaultUsageId, DefaultMemberId, PowerUpType.BlackTicket);
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateMrXTeam());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, true).Returns(usage);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdatePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound"),
            domainError => Assert.Fail("Expected Success but got DomainError")
        );
        usage.PowerUpType.Should().Be(data.PowerUpType);
        usage.UsedAt.Should().Be(data.UsedAt);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask UpdatePowerUpUsageAsync_ReturnsNotFound_WhenUnknown()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateMrXTeam());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, true).Returns((PowerUpUsage?) null);

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdatePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ },
            domainError => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask UpdatePowerUpUsageAsync_ReturnsNotFound_WhenPayloadMemberDoesNotMatch()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(99, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdatePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError")
        );
    }

    [Fact]
    internal async ValueTask UpdatePowerUpUsageAsync_ReturnsDomainError_WhenTeamRoleIsNotMrX()
    {
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.BlackTicket, SystemClock.Instance.GetCurrentInstant());
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _gameSessionRepository.GetSessionByIdAsync(DefaultSessionId, false).Returns(CreateActiveSession());
        _teamRepository.GetTeamByIdAsync(DefaultTeamId, false).Returns(CreateDetectiveTeam());

        OneOf<Success, NotFound, DomainError> result = await _sut.UpdatePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, data, true);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got Success"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            domainError => domainError.Code.Should().Be(DomainErrorCodes.PowerUpNotAllowedForTeamRole)
        );
    }

    [Fact]
    internal async ValueTask DeletePowerUpUsageAsync_ReturnsSuccess_WhenFound()
    {
        var usage = CreateUsage(DefaultUsageId, DefaultMemberId);
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, true).Returns(usage);

        OneOf<Success, NotFound> result = await _sut.DeletePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        _powerUpUsageRepository.Received(1).RemovePowerUpUsage(usage);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask DeletePowerUpUsageAsync_ReturnsNotFound_WhenUnknown()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, true).Returns((PowerUpUsage?) null);

        OneOf<Success, NotFound> result = await _sut.DeletePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask DeletePowerUpUsageAsync_ReturnsNotFound_WhenMemberMissing()
    {
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns((TeamMember?) null);

        OneOf<Success, NotFound> result = await _sut.DeletePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, true);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got Success"),
            _ => { /* expected */ }
        );
    }
}
