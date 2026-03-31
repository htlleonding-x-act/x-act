using System.Net;
using System.Net.Http.Json;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class UserControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/users";

    [Fact]
    public async ValueTask GetAllUsers_ReturnsList()
    {
        var response = await ApiClient.GetAsync(BaseUrl, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UserListResponse>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Items.Should().HaveCountGreaterThanOrEqualTo(3);
        content.Items.Should().Contain(user => user.Id == SeedData.HostUserId);
    }

    [Fact]
    public async ValueTask GetUserById_ReturnsUser()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.HostUserId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UserDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Email.Should().Be("host@example.com");
    }

    [Fact]
    public async ValueTask GetUserById_NotFound()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/9999", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddUser_ReturnsCreated()
    {
        var request = new UserAddRequest(
            "new_user",
            "new_user@example.com",
            AccountType.Free,
            null,
            0,
            0
        );

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<UserDetailsDto>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.Username.Should().Be("new_user");
    }

    [Fact]
    public async ValueTask AddUser_BadRequest()
    {
        var request = new UserAddRequest("dup_user", "host@example.com");

        var response = await ApiClient.PostAsJsonAsync(BaseUrl, request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdateUser_NoContent()
    {
        var request = new UserUpdateRequest("updated", "updated@example.com", AccountType.Pro, null, 3, 10);

        var response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.DetectiveUserId}",
            request,
            JsonOptions,
            TestCancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.DetectiveUserId}", TestCancellationToken);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedContent = await updated.Content.ReadFromJsonAsync<UserDetailsDto>(JsonOptions, TestCancellationToken);
        updatedContent.Should().NotBeNull();
        updatedContent.Username.Should().Be("updated");
    }

    [Fact]
    public async ValueTask UpdateUser_NotFound()
    {
        var request = new UserUpdateRequest("updated", "updated@example.com", AccountType.Pro, null, 3, 10);

        var response = await ApiClient.PutAsJsonAsync($"{BaseUrl}/9999", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeleteUser_NoContent()
    {
        var response = await ApiClient.DeleteAsync($"{BaseUrl}/{SeedData.SpectatorUserId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleted = await ApiClient.GetAsync($"{BaseUrl}/{SeedData.SpectatorUserId}", TestCancellationToken);
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedContent = await deleted.Content.ReadFromJsonAsync<UserDetailsDto>(JsonOptions, TestCancellationToken);
        deletedContent.Should().NotBeNull();
        deletedContent.Username.Should().Be($"deleted_user_{SeedData.SpectatorUserId}");
    }

    [Fact]
    public async ValueTask DeleteUser_NotFound()
    {
        var response = await ApiClient.DeleteAsync($"{BaseUrl}/9999", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
