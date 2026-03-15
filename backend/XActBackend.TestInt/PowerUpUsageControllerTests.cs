using System.Net;
using System.Net.Http.Json;
using NodaTime;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class PowerUpUsageControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    [Fact]
    public async ValueTask GetAllPowerUpUsages_ReturnsList()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PowerUpUsageListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().ContainSingle();
        content.Items.Should().Contain(usage => usage.Id == SeedData.PowerUpUsageId);
    }

    [Fact]
    public async ValueTask GetPowerUpUsageById_ReturnsUsage()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/{SeedData.PowerUpUsageId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PowerUpUsageDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.PowerUpType.Should().Be(PowerUpType.BlackTicket);
    }

    [Fact]
    public async ValueTask GetPowerUpUsageById_NotFound()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddPowerUpUsage_ReturnsCreated()
    {
        var request = new PowerUpUsageAddRequest(PowerUpType.DoubleMove, SeedData.BaseInstant.Plus(Duration.FromMinutes(40)));

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<PowerUpUsageDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.PowerUpType.Should().Be(PowerUpType.DoubleMove);
    }

    [Fact]
    public async ValueTask AddPowerUpUsage_BadRequest()
    {
        var request = new PowerUpUsageAddRequest(PowerUpType.BlackTicket, Instant.FromUtc(2026, 1, 1, 10, 0));

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/9999/powerupusages",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdatePowerUpUsage_NoContent()
    {
        var request = new PowerUpUsageUpdateRequest(PowerUpType.BlackTicket, Instant.FromUtc(2026, 1, 1, 10, 30));

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/{SeedData.PowerUpUsageId}",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async ValueTask UpdatePowerUpUsage_NotFound()
    {
        var request = new PowerUpUsageUpdateRequest(PowerUpType.BlackTicket, Instant.FromUtc(2026, 1, 1, 10, 30));

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/9999",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeletePowerUpUsage_NoContent()
    {
        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/{SeedData.PowerUpUsageId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedCheck = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/{SeedData.PowerUpUsageId}",
            TestCancellationToken
        );
        deletedCheck.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeletePowerUpUsage_NotFound()
    {
        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/powerupusages/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
