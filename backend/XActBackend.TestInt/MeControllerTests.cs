using System.Net;
using System.Net.Http.Json;
using XActBackend.Controllers;
using XActBackend.Importer;
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

        var setResponse = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("host_user", "🦊", "#5b7cfa"),
                                                      JsonOptions, TestCancellationToken);

        setResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var withAvatar = await client.GetFromJsonAsync<MyProfileDto>(BaseUrl, JsonOptions, TestCancellationToken);
        withAvatar.Should().NotBeNull();
        withAvatar.AvatarEmoji.Should().Be("🦊");
        withAvatar.AvatarColor.Should().Be("#5B7CFA");

        var clearResponse = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("host_user"),
                                                        JsonOptions, TestCancellationToken);

        clearResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var withoutAvatar = await client.GetFromJsonAsync<MyProfileDto>(BaseUrl, JsonOptions, TestCancellationToken);
        withoutAvatar.Should().NotBeNull();
        withoutAvatar.AvatarEmoji.Should().BeNull();
        withoutAvatar.AvatarColor.Should().BeNull();
    }

    [Theory]
    [InlineData("🦊", "blue")]
    [InlineData("🦊", "#12345G")]
    [InlineData("", "#5B7CFA")]
    [InlineData("this is far too long", "#5B7CFA")]
    public async ValueTask UpdateMyProfile_ReturnsBadRequest_WhenAvatarIsInvalid(string emoji, string color)
    {
        using var client = CreateClientSignedInAs(SeedData.HostUserId);

        var response = await client.PutAsJsonAsync(BaseUrl, new UpdateMyProfileRequest("host_user", emoji, color),
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
}
