using System.Net;
using System.Net.Http.Json;
using NodaTime;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class GameSessionControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    [Fact]
    public async ValueTask GetAllGameSessions_ReturnsList()
    {
        var response = await ApiClient.GetAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GameSessionListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().Contain(item => item.Id == SeedData.SessionId);
    }

    [Fact]
    public async ValueTask GetGameSessionById_ReturnsSession()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.JoinCode.Should().Be(SeedData.SessionJoinCode);
    }

    [Fact]
    public async ValueTask GetGameSessionById_NotFound()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/9999", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetGameSessionByJoinCode_ReturnsSession()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/join/{SeedData.SessionJoinCode}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Id.Should().Be(SeedData.SessionId);
    }

    [Fact]
    public async ValueTask GetGameSessionByJoinCode_NotFound()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/join/MISSING", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddGameSession_ReturnsCreated()
    {
        const int NewHostUserId = 10;
        await ModifyDatabaseContentAsync(context =>
        {
            context.Users.Add(new User
            {
                Id = NewHostUserId,
                Username = "new_host",
                Email = "new_host@example.com",
                AccountType = AccountType.Free,
                TotalWins = 0,
                TotalGamesPlayed = 0,
                CreatedAt = SeedData.BaseInstant
            });

            return new ValueTask(context.SaveChangesAsync());
        });

        var startTime = TestClock.GetCurrentInstant().Plus(Duration.FromHours(1));
        var request = new GameSessionAddRequest(
            NewHostUserId,
            "New Session",
            "NEW123",
            SessionStatus.Waiting,
            startTime,
            startTime.Plus(Duration.FromHours(2)),
            90,
            4
        );

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location.AbsolutePath.Should().Contain(BaseUrl);

        var content = await response.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.SessionName.Should().Be("New Session");
    }

    [Fact]
    public async ValueTask AddGameSession_Conflict_WhenHostAlreadyHasOpenSession()
    {
        var request = new GameSessionAddRequest(SeedData.HostUserId, "Session", "JOIN42");

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask UpdateGameSession_NoContent()
    {
        var updateRequest = new GameSessionUpdateRequest(SeedData.HostUserId, "Updated", "UPD123",
            SessionStatus.Active, SeedData.BaseInstant, SeedData.BaseInstant.Plus(Duration.FromHours(1)), 90, 4);

        var response = await ApiClient.PutAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}", updateRequest, JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async ValueTask UpdateGameSession_NotFound()
    {
        var updateRequest = new GameSessionUpdateRequest(SeedData.HostUserId, "Updated", "UPD123",
            SessionStatus.Active, SeedData.BaseInstant, SeedData.BaseInstant.Plus(Duration.FromHours(1)), 90, 4);

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/9999",
            updateRequest,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteGameSession_NoContent()
    {
        var response = await ApiClient.DeleteAsync($"{BaseUrl}/{SeedData.SessionTwoId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deletedCheck = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionTwoId}", TestCancellationToken);
        deletedCheck.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteGameSession_NotFound()
    {
        var response = await ApiClient.DeleteAsync($"{BaseUrl}/9999", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
