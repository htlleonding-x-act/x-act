using System.Net;
using System.Net.Http.Json;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class GeofencePointControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    [Fact]
    public async ValueTask GetAllGeofencePoints_ReturnsList()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeofencePointListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().HaveCount(2);
        content.Items.Should().Contain(point => point.Id == SeedData.GeofencePointOneId);
    }

    [Fact]
    public async ValueTask GetGeofencePointById_ReturnsPoint()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/{SeedData.GeofencePointOneId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeofencePointDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Id.Should().Be(SeedData.GeofencePointOneId);
    }

    [Fact]
    public async ValueTask GetGeofencePointById_NotFound()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddGeofencePoint_ReturnsCreated()
    {
        var request = new GeofencePointAddRequest(48.22, 16.32, 3);

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<GeofencePointDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.SequenceOrder.Should().Be(3);
    }

    [Fact]
    public async ValueTask AddGeofencePoint_NotFound_WhenSessionNotFound()
    {
        var request = new GeofencePointAddRequest(48.2, 16.3, 1);

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/9999/geofencepoints",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddGeofencePoint_ReturnsUnprocessableEntity_WhenLimitReached()
    {
        // Seed has 2 points for SessionId; fill up to the limit of 10
        await ModifyDatabaseContentAsync(async ctx =>
        {
            for (int i = 3; i <= 10; i++)
            {
                ctx.GeofencePoints.Add(new GeofencePoint
                {
                    SessionId = SeedData.SessionId,
                    Latitude = 48.2 + i * 0.01,
                    Longitude = 16.3 + i * 0.01,
                    SequenceOrder = i,
                });
            }
            await ctx.SaveChangesAsync();
        });

        var request = new GeofencePointAddRequest(48.5, 16.5, 11);

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async ValueTask UpdateGeofencePoint_NoContent()
    {
        var request = new GeofencePointUpdateRequest(48.2, 16.3, 3);

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/{SeedData.GeofencePointOneId}",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async ValueTask UpdateGeofencePoint_NotFound()
    {
        var request = new GeofencePointUpdateRequest(48.2, 16.3, 3);

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/9999",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteGeofencePoint_NoContent()
    {
        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/{SeedData.GeofencePointTwoId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedCheck = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/{SeedData.GeofencePointTwoId}",
            TestCancellationToken
        );
        deletedCheck.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteGeofencePoint_NotFound()
    {
        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/geofencepoints/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
