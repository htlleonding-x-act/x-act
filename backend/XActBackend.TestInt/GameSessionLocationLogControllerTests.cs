using System.Net;
using System.Net.Http.Json;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class GameSessionLocationLogControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    [Fact]
    public async ValueTask GetAllLocationLogsBySession_ReturnsList()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/locationlogs",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LocationLogListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().HaveCount(2);
        content.Items.Should().Contain(log => log.MemberId == SeedData.DetectiveMemberId);
    }
}
