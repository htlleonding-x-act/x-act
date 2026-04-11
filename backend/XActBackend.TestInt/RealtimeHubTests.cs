using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using XActBackend.Controllers;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.Realtime;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class RealtimeHubTests : SeededWebApiTestBase
{
    private readonly WebApiTestFixture _fixture;

    private const string BaseUrl = "api/gamesessions";

    public RealtimeHubTests(WebApiTestFixture fixture) : base(fixture)
    {
        _fixture = fixture;
    }

    private ValueTask ActivateSeedSessionAsync() =>
        ModifyDatabaseContentAsync(context =>
        {
            GameSession session = context.GameSessions.Single(session => session.Id == SeedData.SessionId);
            session.Status = SessionStatus.Active;

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

    [Fact]
    public async ValueTask SubscribeSession_SendsInitialSnapshot()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);

        GameSessionSnapshot snapshot = await realtimeClient.SubscribeSessionAndGetSnapshotAsync(SeedData.SessionId, TestCancellationToken);

        snapshot.Should().NotBeNull();
        snapshot.SessionId.Should().Be(SeedData.SessionId);
        snapshot.Teams.Should().NotBeEmpty();
        snapshot.Members.Should().NotBeEmpty();
    }

    [Fact]
    public async ValueTask AddTeamMember_PublishesJoinedEvent()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        TeamMemberAddRequest request = new(
            UserId: null,
            GuestName: "Realtime Guest",
            IsTeamLeader: false,
            CurrentLatitude: 48.25,
            CurrentLongitude: 16.35,
            LastUpdated: SeedData.BaseInstant);

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.TeamMemberJoined);

        ((JsonElement)realtimeEvent.Payload).GetProperty("sessionId").GetInt32().Should().Be(SeedData.SessionId);
        ((JsonElement)realtimeEvent.Payload).GetProperty("teamId").GetInt32().Should().Be(SeedData.DetectiveTeamId);
    }

    [Fact]
    public async ValueTask StartGameSession_PublishesStartedEvent()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        HttpResponseMessage response = await ApiClient.PostAsync(
            $"{BaseUrl}/{SeedData.SessionId}/start",
            content: null,
            cancellationToken: TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.GameSessionStarted);
        ((JsonElement)realtimeEvent.Payload).GetProperty("sessionId").GetInt32().Should().Be(SeedData.SessionId);
    }

    [Fact]
    public async ValueTask AddLocationLog_PublishesLocationEvent()
    {
        await ActivateSeedSessionAsync();

        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        var request = new LocationLogAddRequest(
            SeedData.BaseInstant.Plus(Duration.FromMinutes(45)),
            48.25,
            16.35,
            4.0,
            TransportMode.Tram,
            false);

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.DetectiveMemberId}/locationlogs",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.LocationLogRecorded);

        JsonElement payload = (JsonElement)realtimeEvent.Payload;
        payload.GetProperty("sessionId").GetInt32().Should().Be(SeedData.SessionId);
        payload.GetProperty("memberId").GetInt32().Should().Be(SeedData.DetectiveMemberId);
    }
}
