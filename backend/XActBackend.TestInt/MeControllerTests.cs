using System.Net;
using System.Net.Http.Json;
using NodaTime;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class MeControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/users/me";

    [Fact]
    public async ValueTask GetMyProfile_ReturnsUnauthorized_WhenNotSignedIn()
    {
        var response = await ApiClient.GetAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async ValueTask GetMyProfile_ReturnsSignedInUser()
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.GetAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<MyProfileDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Id.Should().Be(SeedData.HostUserId);
        content.Username.Should().Be("host_user");
        content.Email.Should().Be("host@example.com");
    }

    [Fact]
    public async ValueTask GetMyProfile_ReturnsNotFound_WhenSubjectHasNoUser()
    {
        using var client = CreateClientSignedInAs("unknown-subject");

        var response = await client.GetAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdateMyProfile_RenamesSignedInUser()
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("  renamed_host "),
                                                   JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var profile = await client.GetFromJsonAsync<MyProfileDto>(BaseUrl, JsonOptions, TestCancellationToken);
        profile.Should().NotBeNull();
        profile.Username.Should().Be("renamed_host");
    }

    [Fact]
    public async ValueTask UpdateMyProfile_StoresAndClearsAvatar()
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var setResponse = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("host_user", "fox"),
                                                      JsonOptions, TestCancellationToken);

        setResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var withAvatar = await client.GetFromJsonAsync<MyProfileDto>(BaseUrl, JsonOptions, TestCancellationToken);
        withAvatar.Should().NotBeNull();
        withAvatar.AvatarIcon.Should().Be("fox");

        var clearResponse = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("host_user"),
                                                        JsonOptions, TestCancellationToken);

        clearResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var withoutAvatar = await client.GetFromJsonAsync<MyProfileDto>(BaseUrl, JsonOptions, TestCancellationToken);
        withoutAvatar.Should().NotBeNull();
        withoutAvatar.AvatarIcon.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("🦊")]
    [InlineData("Fox")]
    [InlineData("a_key_that_is_far_too_long_to_fit")]
    public async ValueTask UpdateMyProfile_ReturnsBadRequest_WhenAvatarIconIsInvalid(string icon)
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("host_user", icon),
                                                   JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdateMyProfile_ReturnsBadRequest_WhenUsernameIsBlank()
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("   "),
                                                   JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdateMyProfile_ReturnsConflict_WhenAnotherUserHasTheName()
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("detective_user"),
                                                   JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask UpdateMyProfile_ReturnsUnauthorized_WhenNotSignedIn()
    {
        var response = await ApiClient.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("renamed"),
                                                      JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async ValueTask DeleteMyAccount_SoftDeletesSignedInUser()
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.DeleteAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var user = await ApiClient.GetFromJsonAsync<UserDetailsDto>($"api/users/{SeedData.HostUserId}", JsonOptions,
                                                                    TestCancellationToken);
        user.Should().NotBeNull();
        user.Username.Should().StartWith("deleted_user_");
    }

    [Fact]
    public async ValueTask DeleteMyAccount_ReturnsUnauthorized_WhenNotSignedIn()
    {
        var response = await ApiClient.DeleteAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async ValueTask GetMyStats_ReturnsUnauthorized_WhenNotSignedIn()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/stats", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async ValueTask GetMyStats_ReturnsZeros_WhenNoMatchIsFinished()
    {
        using var client = CreateClientSignedInAs(SeedData.DetectiveUserId);

        var stats = await client.GetFromJsonAsync<PlayerStatsDto>($"{BaseUrl}/stats", JsonOptions, TestCancellationToken);

        stats.Should().Be(new PlayerStatsDto(0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public async ValueTask GetMyStats_MatchesTheEndScreenResults()
    {
        await FinishSeededSessionAsync();
        using var client = CreateClientSignedInAs(SeedData.DetectiveUserId);

        var stats = await client.GetFromJsonAsync<PlayerStatsDto>($"{BaseUrl}/stats", JsonOptions, TestCancellationToken);
        var results = await ApiClient.GetFromJsonAsync<GameResultsDto>($"api/gamesessions/{SeedData.SessionId}/results",
                                                                        JsonOptions, TestCancellationToken);

        stats.Should().NotBeNull();
        results.Should().NotBeNull();
        ResultMemberDto detective = results.Members.Single(m => m.MemberId == SeedData.DetectiveMemberId);
        stats.GamesPlayed.Should().Be(1);
        stats.Wins.Should().Be(0);
        stats.DistanceMeters.Should().BeGreaterThan(0);
        // the results dto rounds to a tenth of a metre
        stats.DistanceMeters.Should().BeApproximately(detective.Stats.DistanceMeters, 0.05);
    }

    [Fact]
    public async ValueTask GetMyMatches_ReturnsFinishedMatchFromTheUsersView()
    {
        await FinishSeededSessionAsync();
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var history = await client.GetFromJsonAsync<MatchHistoryResponse>($"{BaseUrl}/matches", JsonOptions,
                                                                           TestCancellationToken);

        history.Should().NotBeNull();
        MatchSummaryDto match = history.Items.Should().ContainSingle().Subject;
        match.SessionId.Should().Be(SeedData.SessionId);
        match.MemberId.Should().Be(SeedData.HostMemberId);
        match.Won.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MatchHistoryQuery.MaxLimit + 1)]
    public async ValueTask GetMyMatches_ReturnsBadRequest_WhenLimitIsOutOfRange(int limit)
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.GetAsync($"{BaseUrl}/matches?limit={limit}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private ValueTask FinishSeededSessionAsync() =>
        ModifyDatabaseContentAsync(context =>
        {
            GameSession session = context.GameSessions.Single(s => s.Id == SeedData.SessionId);
            session.Status = SessionStatus.Finished;
            session.EndTime = SeedData.BaseInstant.Plus(Duration.FromHours(2));

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });
}
