using AwesomeAssertions;
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

    private readonly IPowerUpUsageRepository _powerUpUsageRepository;
    private readonly PowerUpUsageService _sut;
    private readonly IUnitOfWork _uow;

    public PowerUpUsageServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _powerUpUsageRepository = Substitute.For<IPowerUpUsageRepository>();
        _uow.PowerUpUsageRepository.Returns(_powerUpUsageRepository);
        _sut = new PowerUpUsageService(_uow);
    }

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
        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, false).Returns(usages);

        var result = await _sut.GetUsagesByMemberIdAsync(DefaultMemberId, false);

        result.Should().BeEquivalentTo(usages);
    }

    [Fact]
    internal async ValueTask GetPowerUpUsageByIdAsync_ReturnsUsage_WhenFound()
    {
        var usage = CreateUsage();
        var usages = new List<PowerUpUsage> { usage };
        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, false).Returns(usages);

        OneOf<PowerUpUsage, NotFound> result = await _sut.GetPowerUpUsageByIdAsync(DefaultMemberId, DefaultUsageId, false);

        result.Switch(
            usage => usage.Should().BeEquivalentTo(usage),
            notFound => Assert.Fail("Expected PowerUpUsage but got NotFound")
        );
    }

    [Fact]
    internal async ValueTask GetPowerUpUsageByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        var usages = new List<PowerUpUsage>();
        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, false).Returns(usages);

        OneOf<PowerUpUsage, NotFound> result = await _sut.GetPowerUpUsageByIdAsync(DefaultMemberId, DefaultUsageId, false);

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
        var usages = new List<PowerUpUsage> { usage };
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());

        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, true).Returns(usages);

        OneOf<Success, NotFound> result = await _sut.UpdatePowerUpUsageAsync(DefaultUsageId, data, true);

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
        var usages = new List<PowerUpUsage>();
        var data = new IPowerUpUsageService.PowerUpUsageData(DefaultMemberId, PowerUpType.DoubleMove, SystemClock.Instance.GetCurrentInstant());

        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, true).Returns(usages);

        OneOf<Success, NotFound> result = await _sut.UpdatePowerUpUsageAsync(DefaultUsageId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    internal async ValueTask DeletePowerUpUsageAsync_ReturnsSuccess_WhenFound()
    {
        var usage = CreateUsage(DefaultUsageId, DefaultMemberId);
        var usages = new List<PowerUpUsage> { usage };
        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, true).Returns(usages);

        OneOf<Success, NotFound> result = await _sut.DeletePowerUpUsageAsync(DefaultMemberId, DefaultUsageId, true);

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
        var usages = new List<PowerUpUsage>();
        _powerUpUsageRepository.GetUsagesByMemberIdAsync(DefaultMemberId, true).Returns(usages);

        OneOf<Success, NotFound> result = await _sut.DeletePowerUpUsageAsync(DefaultMemberId, DefaultUsageId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }
}
