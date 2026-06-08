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

    // --- StartGameSession ---

    [Fact]
    public async ValueTask StartGameSession_NoContent()
    {
        var response = await ApiClient.PostAsync($"{BaseUrl}/{SeedData.SessionId}/start", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var checkResponse = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionId}", TestCancellationToken);
        var content = await checkResponse.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        content!.Status.Should().Be(SessionStatus.Active);
        content.StartTime.Should().NotBeNull();
    }

    [Fact]
    public async ValueTask StartGameSession_NotFound()
    {
        var response = await ApiClient.PostAsync($"{BaseUrl}/9999/start", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask StartGameSession_Conflict_WhenNotWaiting()
    {
        var response = await ApiClient.PostAsync($"{BaseUrl}/{SeedData.SessionTwoId}/start", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- EndGameSession ---

    [Fact]
    public async ValueTask EndGameSession_NoContent()
    {
        var response = await ApiClient.PostAsync($"{BaseUrl}/{SeedData.SessionTwoId}/end", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var checkResponse = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionTwoId}", TestCancellationToken);
        var content = await checkResponse.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        content!.Status.Should().Be(SessionStatus.Finished);
        content.EndTime.Should().NotBeNull();
    }

    [Fact]
    public async ValueTask EndGameSession_NotFound()
    {
        var response = await ApiClient.PostAsync($"{BaseUrl}/9999/end", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask EndGameSession_Conflict_WhenNotActive()
    {
        var response = await ApiClient.PostAsync($"{BaseUrl}/{SeedData.SessionId}/end", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- RematchGameSession ---

    private ValueTask FinishSeededSessionAsync() =>
        ModifyDatabaseContentAsync(context =>
        {
            GameSession session = context.GameSessions.Single(s => s.Id == SeedData.SessionId);
            session.Status = SessionStatus.Finished;
            session.EndTime = SeedData.BaseInstant.Plus(Duration.FromHours(2));

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

    [Fact]
    public async ValueTask RematchGameSession_Created_WhenFinished()
    {
        await FinishSeededSessionAsync();

        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/rematch",
            new GameSessionRematchRequest("REMAT1"),
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var rematch = await response.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        rematch.Should().NotBeNull();
        rematch.Id.Should().NotBe(SeedData.SessionId);
        rematch.Status.Should().Be(SessionStatus.Waiting);
        rematch.JoinCode.Should().Be("REMAT1");
        rematch.SessionName.Should().Be("Alpha Session");
        rematch.HostUserId.Should().Be(SeedData.HostUserId);
        rematch.PlannedDurationMinutes.Should().Be(120);
        rematch.MrXRevealInterval.Should().Be(5);
        rematch.StartTime.Should().BeNull();
        rematch.EndTime.Should().BeNull();

        // The finished session is preserved as history.
        var original = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionId}", TestCancellationToken);
        var originalContent = await original.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        originalContent!.Status.Should().Be(SessionStatus.Finished);

        // The rematch is an independent session reachable by its new join code.
        var byCode = await ApiClient.GetAsync($"{BaseUrl}/join/REMAT1", TestCancellationToken);
        byCode.StatusCode.Should().Be(HttpStatusCode.OK);
        var byCodeContent = await byCode.Content.ReadFromJsonAsync<GameSessionDetailsDto>(JsonOptions, TestCancellationToken);
        byCodeContent!.Id.Should().Be(rematch.Id);
    }

    [Fact]
    public async ValueTask RematchGameSession_NotFound()
    {
        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/9999/rematch",
            new GameSessionRematchRequest("REMAT1"),
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask RematchGameSession_Conflict_WhenNotFinished()
    {
        // The seeded session is still Waiting, so it cannot be used for a rematch.
        var response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/rematch",
            new GameSessionRematchRequest("REMAT1"),
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // --- CatchMrX ---

    private async ValueTask ActivateSeededSessionAsync()
    {
        await ModifyDatabaseContentAsync(context =>
        {
            var session = context.GameSessions.Find(SeedData.SessionId);
            session!.Status = SessionStatus.Active;
            return new ValueTask(context.SaveChangesAsync());
        });
    }

    [Fact]
    public async ValueTask CatchMrX_NoContent_AndSwapsRoles()
    {
        await ActivateSeededSessionAsync();

        var request = new CatchMrXRequest(SeedData.DetectiveTeamId);
        var response = await ApiClient.PostAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}/catch", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The catching detective team is now Mr.X, and the former Mr.X team is now a detective team.
        var formerMrX = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.MrXTeamId}", TestCancellationToken);
        var newMrX = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}", TestCancellationToken);
        var formerMrXTeam = await formerMrX.Content.ReadFromJsonAsync<TeamDetailsDto>(JsonOptions, TestCancellationToken);
        var newMrXTeam = await newMrX.Content.ReadFromJsonAsync<TeamDetailsDto>(JsonOptions, TestCancellationToken);
        formerMrXTeam!.Role.Should().Be(TeamRole.Detective);
        newMrXTeam!.Role.Should().Be(TeamRole.MrX);
    }

    [Fact]
    public async ValueTask CatchMrX_BadRequest_WhenCatchingTeamIdInvalid()
    {
        await ActivateSeededSessionAsync();

        var request = new CatchMrXRequest(0);
        var response = await ApiClient.PostAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}/catch", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CatchMrX_NotFound()
    {
        var request = new CatchMrXRequest(SeedData.DetectiveTeamId);
        var response = await ApiClient.PostAsJsonAsync($"{BaseUrl}/9999/catch", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CatchMrX_Conflict_WhenNotActive()
    {
        var request = new CatchMrXRequest(SeedData.DetectiveTeamId);
        var response = await ApiClient.PostAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}/catch", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask CatchMrX_Conflict_WhenCatchingTeamIsNotDetective()
    {
        await ActivateSeededSessionAsync();

        // The current Mr.X team is not a detective team and therefore cannot "catch" Mr.X.
        var request = new CatchMrXRequest(SeedData.MrXTeamId);
        var response = await ApiClient.PostAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}/catch", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
