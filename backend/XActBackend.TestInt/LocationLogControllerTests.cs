using System.Net;
using System.Net.Http.Json;
using NodaTime;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class LocationLogControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    private ValueTask ActivateSeedSessionAsync() =>
        ModifyDatabaseContentAsync(context =>
        {
            GameSession session = context.GameSessions.Single(session => session.Id == SeedData.SessionId);
            session.Status = SessionStatus.Active;

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

    [Fact]
    public async ValueTask GetAllLocationLogs_ReturnsList()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LocationLogListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().HaveCount(2);
        content.Items.Should().Contain(log => log.Id == SeedData.LocationLogOneId);
    }

    [Fact]
    public async ValueTask GetLocationLogById_ReturnsLog()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/{SeedData.LocationLogOneId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LocationLogDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Id.Should().Be(SeedData.LocationLogOneId);
    }

    [Fact]
    public async ValueTask GetLocationLogById_NotFound()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddLocationLog_ReturnsCreated()
    {
        await ActivateSeedSessionAsync();

        var request = new LocationLogAddRequest(
            SeedData.BaseInstant.Plus(Duration.FromMinutes(45)),
            48.25,
            16.35,
            4.0,
            TransportMode.Tram,
            false
        );

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<LocationLogDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.MemberId.Should().Be(SeedData.DetectiveMemberId);
    }

    [Fact]
    public async ValueTask AddLocationLog_NotFound_WhenMemberIsMissing()
    {
        var request = new LocationLogAddRequest(
            Instant.FromUtc(2026, 1, 1, 9, 0),
            48.2,
            16.3,
            5.5,
            TransportMode.Foot,
            false
        );

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/9999/locationlogs",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdateLocationLog_NoContent()
    {
        await ActivateSeedSessionAsync();

        var request = new LocationLogUpdateRequest(
            Instant.FromUtc(2026, 1, 1, 9, 0),
            48.2,
            16.3,
            3.0,
            TransportMode.Bus,
            true
        );

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/{SeedData.LocationLogOneId}",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async ValueTask UpdateLocationLog_NotFound()
    {
        await ActivateSeedSessionAsync();

        var request = new LocationLogUpdateRequest(
            Instant.FromUtc(2026, 1, 1, 9, 0),
            48.2,
            16.3,
            3.0,
            TransportMode.Bus,
            true
        );

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/9999",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteLocationLog_NoContent()
    {
        await ActivateSeedSessionAsync();

        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/{SeedData.LocationLogTwoId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedCheck = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/{SeedData.LocationLogTwoId}",
            TestCancellationToken
        );
        deletedCheck.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteLocationLog_NotFound()
    {
        await ActivateSeedSessionAsync();

        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
