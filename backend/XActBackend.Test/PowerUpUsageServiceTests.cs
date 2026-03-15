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
    private readonly PowerUpUsageService _sut;
    private readonly IUnitOfWork _uow;

    public PowerUpUsageServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _powerUpUsageRepository = Substitute.For<IPowerUpUsageRepository>();
        _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
        _uow.PowerUpUsageRepository.Returns(_powerUpUsageRepository);
        _uow.TeamMemberRepository.Returns(_teamMemberRepository);
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
    internal async ValueTask GetPowerUpUsageByIdAsync_ReturnsUsage_WhenFound()
    {
        var usage = CreateUsage();
        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, false).Returns(usage);

        OneOf<PowerUpUsage, NotFound> result = await _sut.GetPowerUpUsageByIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, false);

        result.Switch(
            usage => usage.Should().BeEquivalentTo(usage),
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
    internal async ValueTask AddPowerUpUsageAsync_ReturnsAddedUsage()
    {
        var usedAt = SystemClock.Instance.GetCurrentInstant();
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, usedAt);
        var usage = new PowerUpUsage { Id = DefaultUsageId, MemberId = data.MemberId, PowerUpType = data.PowerUpType, UsedAt = data.UsedAt };

        _powerUpUsageRepository.AddPowerUpUsage(data.MemberId, data.PowerUpType, data.UsedAt).Returns(usage);

        OneOf<PowerUpUsage, Error> result = await _sut.AddPowerUpUsageAsync(data);

        result.Switch(
            usage => usage.Should().BeEquivalentTo(usage),
            error => Assert.Fail("Expected PowerUpUsage but got Error")
        );
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    internal async ValueTask UpdatePowerUpUsageAsync_ReturnsSuccess_WhenFound()
    {
        var usage = CreateUsage(DefaultUsageId, DefaultMemberId, PowerUpType.BlackTicket);
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());

        _teamMemberRepository.GetMemberBySessionAndTeamIdAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, false).Returns(CreateMember());
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, true).Returns(usage);

        OneOf<Success, NotFound> result = await _sut.UpdatePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
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
        _powerUpUsageRepository.GetUsageByMemberAndIdAsync(DefaultMemberId, DefaultUsageId, true).Returns((PowerUpUsage?) null);

        OneOf<Success, NotFound> result = await _sut.UpdatePowerUpUsageAsync(DefaultSessionId, DefaultTeamId, DefaultMemberId, DefaultUsageId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
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
}
