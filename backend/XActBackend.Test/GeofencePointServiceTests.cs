using AwesomeAssertions;
using Microsoft.Extensions.Logging;
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

namespace XActBackend.Test;

public sealed class GeofencePointServiceTests
{
    private const int DefaultSessionId = 1;
    private const int DefaultPointId = 1;

    private readonly IGeofencePointRepository _geofencePointRepository;
    private readonly GeoFencePointService _sut;
    private readonly IUnitOfWork _uow;

    public GeofencePointServiceTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _geofencePointRepository = Substitute.For<IGeofencePointRepository>();
        _uow.GeofencePointRepository.Returns(_geofencePointRepository);
        var logger = Substitute.For<ILogger<GeoFencePointService>>();
        _sut = new GeoFencePointService(_uow, logger);
    }

    private static GeofencePoint CreatePoint(
        int id = DefaultPointId,
        int sessionId = DefaultSessionId,
        double latitude = 10.0,
        double longitude = 20.0,
        int sequenceOrder = 1
    ) =>
        new()
        {
            Id = id,
            SessionId = sessionId,
            Latitude = latitude,
            Longitude = longitude,
            SequenceOrder = sequenceOrder,
        };

    private static List<GeofencePoint> CreatePoints() =>
        [
            CreatePoint(DefaultPointId, DefaultSessionId, 10.0, 20.0, 1),
            CreatePoint(2, DefaultSessionId, 15.0, 25.0, 2),
        ];

    [Fact]
    public async ValueTask GetAllPointsBySessionIdAsync_ReturnsPoints()
    {
        var points = CreatePoints();
        _geofencePointRepository.GetPointsBySessionIdAsync(DefaultSessionId, false).Returns(points);

        var result = await _sut.GetAllPointsBySessionIdAsync(DefaultSessionId, false);

        result.Should().BeEquivalentTo(points);
    }

    [Fact]
    public async ValueTask GetGeofencePointByIdAsync_ReturnsPoint_WhenFound()
    {
        var point = CreatePoint();
        _geofencePointRepository.GetPointBySessionAndIdAsync(DefaultSessionId, DefaultPointId, false).Returns(point);

        var result = await _sut.GetGeofencePointByIdAsync(DefaultSessionId, DefaultPointId, false);

        result.Switch(
            found => found.Should().BeEquivalentTo(point),
            notFound => Assert.Fail("Expected GeofencePoint but got NotFound")
        );
    }

    [Fact]
    public async ValueTask GetGeofencePointByIdAsync_ReturnsNotFound_WhenUnknown()
    {
        _geofencePointRepository.GetPointBySessionAndIdAsync(DefaultSessionId, DefaultPointId, false).Returns((GeofencePoint?) null);

        var result = await _sut.GetGeofencePointByIdAsync(DefaultSessionId, DefaultPointId, false);

        result.Switch(
            point => Assert.Fail("Expected NotFound but got GeofencePoint"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask AddGeofencePointAsync_ReturnsAddedPoint()
    {
        var data = new IGeofencePointService.GeofencePointData(DefaultSessionId, 12.34, 56.78, 1);
        var point = CreatePoint(DefaultPointId, data.SessionId, data.Latitude, data.Longitude, data.SequenceOrder);

        _geofencePointRepository.AddGeofencePoint(data.SessionId, data.Latitude, data.Longitude, data.SequenceOrder).Returns(point);

        var result = await _sut.AddGeofencePointAsync(data);

        result.Switch(
            addedPoint => addedPoint.Should().BeEquivalentTo(point),
            error => Assert.Fail("Expected GeofencePoint but got Error")
        );
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateGeofencePointAsync_ReturnsSuccess_WhenFound()
    {
        var point = CreatePoint(DefaultPointId, DefaultSessionId, 0.0, 0.0, 0);
        var data = new IGeofencePointService.GeofencePointData(DefaultSessionId, 10.0, 20.0, 2);
        _geofencePointRepository.GetPointBySessionAndIdAsync(DefaultSessionId, DefaultPointId, true).Returns(point);

        var result = await _sut.UpdateGeofencePointAsync(DefaultPointId, data, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        point.Latitude.Should().Be(data.Latitude);
        point.Longitude.Should().Be(data.Longitude);
        point.SequenceOrder.Should().Be(data.SequenceOrder);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask UpdateGeofencePointAsync_ReturnsNotFound_WhenUnknown()
    {
        var data = new IGeofencePointService.GeofencePointData(DefaultSessionId, 10.0, 20.0, 2);
        _geofencePointRepository.GetPointBySessionAndIdAsync(DefaultSessionId, DefaultPointId, true).Returns((GeofencePoint?) null);

        OneOf<Success, NotFound> result = await _sut.UpdateGeofencePointAsync(DefaultPointId, data, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }

    [Fact]
    public async ValueTask DeleteGeofencePointAsync_ReturnsSuccess_WhenFound()
    {
        var point = CreatePoint();
        _geofencePointRepository.GetPointBySessionAndIdAsync(DefaultSessionId, DefaultPointId, true).Returns(point);

        OneOf<Success, NotFound> result = await _sut.DeleteGeofencePointAsync(DefaultSessionId, DefaultPointId, true);

        result.Switch(
            success => { /* expected */ },
            notFound => Assert.Fail("Expected Success but got NotFound")
        );
        _geofencePointRepository.Received(1).RemoveGeofencePoint(point);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async ValueTask DeleteGeofencePointAsync_ReturnsNotFound_WhenUnknown()
    {
        _geofencePointRepository.GetPointBySessionAndIdAsync(DefaultSessionId, DefaultPointId, true).Returns((GeofencePoint?) null);

        OneOf<Success, NotFound> result = await _sut.DeleteGeofencePointAsync(DefaultSessionId, DefaultPointId, true);

        result.Switch(
            success => Assert.Fail("Expected NotFound but got Success"),
            notFound => { /* expected */ }
        );
    }
}