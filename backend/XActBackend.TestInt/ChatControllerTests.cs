using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using XActBackend.Controllers;
using XActBackend.Core.Realtime;
using XActBackend.Importer;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class ChatControllerTests : SeededWebApiTestBase
{
    private readonly WebApiTestFixture _fixture;

    private const string BaseUrl = "api/gamesessions";

    public ChatControllerTests(WebApiTestFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async ValueTask GetAllMessages_ReturnsSeededGlobalMessage()
    {
        HttpResponseMessage response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/all",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ChatMessageListResponse? body = await response.Content.ReadFromJsonAsync<ChatMessageListResponse>(JsonOptions, TestCancellationToken);

        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle();
        body.Items[0].TeamId.Should().BeNull();
        body.Items[0].Content.Should().Be("Welcome everyone, good luck!");
    }

    [Fact]
    public async ValueTask GetTeamMessages_ReturnsSeededTeamMessage()
    {
        HttpResponseMessage response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/teams/{SeedData.DetectiveTeamId}",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ChatMessageListResponse? body = await response.Content.ReadFromJsonAsync<ChatMessageListResponse>(JsonOptions, TestCancellationToken);

        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle();
        body.Items[0].TeamId.Should().Be(SeedData.DetectiveTeamId);
    }

    [Fact]
    public async ValueTask GetTeamMessages_ReturnsNotFound_ForTeamOutsideSession()
    {
        HttpResponseMessage response = await ApiClient.GetAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/teams/{SeedData.SessionTwoTeamId}",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask PostAllMessage_CreatesMessageAndPublishesToSession()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        var request = new ChatMessagePostRequest(SeedData.DetectiveMemberId, "Hello all");

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/all",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.ChatMessagePosted);

        JsonElement payload = (JsonElement)realtimeEvent.Payload;
        payload.GetProperty("sessionId").GetInt32().Should().Be(SeedData.SessionId);
        payload.GetProperty("content").GetString().Should().Be("Hello all");
        payload.GetProperty("teamId").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("senderName").GetString().Should().Be("detective_user");
    }

    [Fact]
    public async ValueTask PostTeamMessage_PublishesOnlyToTeamChannel()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);
        await realtimeClient.JoinTeamChannelAsync(SeedData.SessionId, SeedData.DetectiveTeamId, TestCancellationToken);

        var request = new ChatMessagePostRequest(SeedData.DetectiveMemberId, "Team only");

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/teams/{SeedData.DetectiveTeamId}",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.ChatMessagePosted);

        JsonElement payload = (JsonElement)realtimeEvent.Payload;
        payload.GetProperty("teamId").GetInt32().Should().Be(SeedData.DetectiveTeamId);
        payload.GetProperty("content").GetString().Should().Be("Team only");
    }

    [Fact]
    public async ValueTask PostTeamMessage_NotDeliveredToSessionOnlySubscriber()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        // Subscribe to the session group only; do NOT join the team channel.
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        var request = new ChatMessagePostRequest(SeedData.DetectiveMemberId, "Secret team message");

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/teams/{SeedData.DetectiveTeamId}",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // A session-only subscriber must not receive the private team message.
        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(1), TestCancellationToken);

        realtimeEvent.Should().BeNull();
    }

    [Fact]
    public async ValueTask PostTeamMessage_ReturnsForbidden_WhenSenderNotOnTeam()
    {
        // Host member belongs to the Mr.X team, not the detective team.
        var request = new ChatMessagePostRequest(SeedData.HostMemberId, "I should not be here");

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/teams/{SeedData.DetectiveTeamId}",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async ValueTask PostAllMessage_ReturnsBadRequest_WhenContentEmpty()
    {
        var request = new ChatMessagePostRequest(SeedData.DetectiveMemberId, "   ");

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/all",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask PostAllMessage_ReturnsNotFound_WhenMemberOutsideSession()
    {
        var request = new ChatMessagePostRequest(SeedData.SessionTwoMemberId, "Wrong session");

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/chat/all",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
