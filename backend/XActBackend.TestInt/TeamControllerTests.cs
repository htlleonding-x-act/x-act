using System.Net;
using System.Net.Http.Json;
using NodaTime;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class TeamControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    [Fact]
    public async ValueTask GetTeamsBySessionId_ReturnsList()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<TeamListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().HaveCount(2);
        content.Items.Should().Contain(team => team.Id == SeedData.MrXTeamId);
    }

    [Fact]
    public async ValueTask GetTeamById_ReturnsTeam()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<TeamDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.TeamName.Should().Be("Detectives");
    }

    [Fact]
    public async ValueTask GetTeamById_NotFound()
    {
        var response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddTeam_ReturnsCreated()
    {
        var request = new TeamAddRequest("Scouts", TeamRole.Spectator, "#abcdef", false);

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<TeamDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.TeamName.Should().Be("Scouts");
    }

    [Fact]
    public async ValueTask AddTeam_BadRequest()
    {
        var request = new TeamAddRequest("Team", TeamRole.Detective, "#ff0000");

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/9999/teams",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdateTeam_NoContent()
    {
        var request = new TeamUpdateRequest("Updated", TeamRole.Detective, "#00ff00", true);

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async ValueTask UpdateTeam_NotFound()
    {
        var request = new TeamUpdateRequest("Updated", TeamRole.MrX, "#00ff00", true);

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/9999",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteTeam_NoContent()
    {
        const int EmptyTeamId = 99;
        await ModifyDatabaseContentAsync(context =>
        {
            context.Teams.Add(new Team
            {
                Id = EmptyTeamId,
                SessionId = SeedData.SessionId,
                TeamName = "Temp Team",
                Role = TeamRole.Spectator,
                ColorCode = "#123456",
                IsCaught = false,
            });

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{EmptyTeamId}",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedCheck = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{EmptyTeamId}",
            TestCancellationToken
        );
        deletedCheck.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteTeam_NotFound()
    {
        var response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/9999",
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
