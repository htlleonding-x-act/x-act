using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using XActBackend.Controllers;
using XActBackend.Core.Realtime;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
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

    [Fact]
    public async ValueTask AddTeam_PublishesTeamAddedEvent()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        TeamAddRequest request = new(
            TeamName: "Realtime Team",
            Role: TeamRole.Detective,
            ColorCode: "#112233",
            IsCaught: false,
            MaxPlayerCount: 5);

        HttpResponseMessage response = await ApiClient.PostAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.TeamAdded);

        JsonElement payload = (JsonElement)realtimeEvent.Payload;
        payload.GetProperty("sessionId").GetInt32().Should().Be(SeedData.SessionId);
        payload.GetProperty("maxPlayerCount").GetInt32().Should().Be(5);
    }

    [Fact]
    public async ValueTask UpdateTeam_PublishesTeamUpdatedEvent()
    {
        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        TeamUpdateRequest request = new(
            TeamName: "Detectives Prime",
            Role: TeamRole.Detective,
            ColorCode: "#123456",
            IsCaught: false,
            MaxPlayerCount: 7);

        HttpResponseMessage response = await ApiClient.PutAsJsonAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}",
            request,
            JsonOptions,
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.TeamUpdated);

        JsonElement payload = (JsonElement)realtimeEvent.Payload;
        payload.GetProperty("teamId").GetInt32().Should().Be(SeedData.DetectiveTeamId);
        payload.GetProperty("maxPlayerCount").GetInt32().Should().Be(7);
    }

    [Fact]
    public async ValueTask DeleteTeam_PublishesTeamDeletedEvent()
    {
        const int TeamId = 300;

        await ModifyDatabaseContentAsync(context =>
        {
            context.Teams.Add(new Team
            {
                Id = TeamId,
                SessionId = SeedData.SessionId,
                TeamName = "Disposable Team",
                Role = TeamRole.Detective,
                ColorCode = "#445566",
                MaxPlayerCount = 4,
                IsCaught = false,
            });

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

        await using var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken);
        await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

        HttpResponseMessage response = await ApiClient.DeleteAsync(
            $"{BaseUrl}/{SeedData.SessionId}/teams/{TeamId}",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        RealtimeEventEnvelope? realtimeEvent = await realtimeClient.TryReadEventAsync(TimeSpan.FromSeconds(3), TestCancellationToken);

        realtimeEvent.Should().NotBeNull();
        realtimeEvent!.Type.Should().Be(RealtimeEvents.TeamDeleted);

        JsonElement payload = (JsonElement)realtimeEvent.Payload;
        payload.GetProperty("teamId").GetInt32().Should().Be(TeamId);
    }

    [Fact]
    public async ValueTask DisconnectInWaitingLobby_RemovesRegisteredMember()
    {
        await using (var realtimeClient = await SignalRTestClient.ConnectAsync(_fixture, TestCancellationToken))
        {
            await realtimeClient.SubscribeSessionAsync(SeedData.SessionId, TestCancellationToken);

            await realtimeClient.RegisterMemberPresenceAsync(
                SeedData.SessionId,
                SeedData.DetectiveTeamId,
                SeedData.GuestMemberId,
                userId: null,
                guestName: "guest_player",
                TestCancellationToken);
        }

        var memberStillExists = true;
        for (var i = 0; i < 12; i++)
        {
            HttpResponseMessage checkResponse = await ApiClient.GetAsync(
                $"{BaseUrl}/{SeedData.SessionId}/teams/{SeedData.DetectiveTeamId}/members/{SeedData.GuestMemberId}",
                TestCancellationToken);

            if (checkResponse.StatusCode == HttpStatusCode.NotFound)
            {
                memberStillExists = false;
                break;
            }

            await Task.Delay(100, TestCancellationToken);
        }

        memberStillExists.Should().BeFalse();
    }
}
