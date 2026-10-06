using System.Net;
using System.Net.Http.Json;
using NodaTime;
using XActBackend.Controllers;
using XActBackend.Core.Util;
using XActBackend.Importer;
using XActBackend.Persistence.Model;
using XActBackend.TestInt.Util;

namespace XActBackend.TestInt;

public sealed class GameResultsControllerTests(WebApiTestFixture fixture) : SeededWebApiTestBase(fixture)
{
    private const string BaseUrl = "api/gamesessions";

    private static string ResultsUrl(int sessionId) => $"{BaseUrl}/{sessionId}/results";

    private ValueTask FinishSeededSessionAsync() =>
        ModifyDatabaseContentAsync(context =>
        {
            GameSession session = context.GameSessions.Single(s => s.Id == SeedData.SessionId);
            session.Status = SessionStatus.Finished;
            session.EndTime = SeedData.BaseInstant.Plus(Duration.FromHours(2));
            session.EndReason = GameEndReason.HostEnded;

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

    private async ValueTask<GameResultsDto> GetResultsAsync(int sessionId)
    {
        var response = await ApiClient.GetAsync(ResultsUrl(sessionId), TestCancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<GameResultsDto>(JsonOptions, TestCancellationToken);
        results.Should().NotBeNull();

        return results;
    }

    [Fact]
    public async ValueTask GetGameResults_NotFound_WhenSessionMissing()
    {
        var response = await ApiClient.GetAsync(ResultsUrl(9999), TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetGameResults_Conflict_WhenSessionNotFinished()
    {
        // the seeded session is still waiting
        var response = await ApiClient.GetAsync(ResultsUrl(SeedData.SessionId), TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask GetGameResults_ReturnsTeamsMembersAndDisplayNames_WhenFinished()
    {
        await FinishSeededSessionAsync();

        GameResultsDto results = await GetResultsAsync(SeedData.SessionId);

        results.SessionId.Should().Be(SeedData.SessionId);
        results.EndReason.Should().Be(GameEndReason.HostEnded);
        results.DurationSeconds.Should().Be(2 * 60 * 60);
        results.WinnerTeamId.Should().Be(SeedData.MrXTeamId);
        results.Teams.Select(t => t.TeamId).Should().Contain([SeedData.MrXTeamId, SeedData.DetectiveTeamId]);
        results.Members.Select(m => m.DisplayName).Should().Contain(["host_user", "detective_user", "Guest A"]);
        results.Members.Single(m => m.MemberId == SeedData.HostMemberId).IsHost.Should().BeTrue();

        ResultMemberDto detective = results.Members.Single(m => m.MemberId == SeedData.DetectiveMemberId);
        detective.Route.Should().HaveCount(2);
        detective.Stats.DistanceMeters.Should().BeGreaterThan(0);
        results.Timeline[0].Type.Should().Be(TimelineEventType.GameStarted);
        results.Timeline[^1].Type.Should().Be(TimelineEventType.GameEnded);
    }

    [Fact]
    public async ValueTask GetGameResults_IncludesCatchHistoryAndEndReason_AfterCatchAndEnd()
    {
        await ModifyDatabaseContentAsync(context =>
        {
            context.GameSessions.Single(s => s.Id == SeedData.SessionId).Status = SessionStatus.Active;
            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

        var catchResponse = await ApiClient.PostAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}/catch",
            new CatchMrXRequest(SeedData.DetectiveTeamId), JsonOptions, TestCancellationToken);
        catchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var endResponse = await ApiClient.PostAsJsonAsync($"{BaseUrl}/{SeedData.SessionId}/end",
            new EndGameSessionRequest(GameEndReason.NoOpponentsLeft), JsonOptions, TestCancellationToken);
        endResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        GameResultsDto results = await GetResultsAsync(SeedData.SessionId);

        results.EndReason.Should().Be(GameEndReason.NoOpponentsLeft);
        results.WinnerTeamId.Should().Be(SeedData.DetectiveTeamId);
        results.MrXPeriods.Select(p => p.TeamId).Should().Equal(SeedData.MrXTeamId, SeedData.DetectiveTeamId);
        TimelineEventDto caught = results.Timeline.Single(e => e.Type == TimelineEventType.MrXCaught);
        caught.TeamId.Should().Be(SeedData.DetectiveTeamId);
        caught.OtherTeamId.Should().Be(SeedData.MrXTeamId);
    }

    [Fact]
    public async ValueTask GetGameResults_IncludesOffensesPowerUpsAndReveals()
    {
        await FinishSeededSessionAsync();
        await ModifyDatabaseContentAsync(context =>
        {
            context.Offenses.Add(new Offense
            {
                SessionId = SeedData.SessionId,
                MemberId = SeedData.DetectiveMemberId,
                Type = OffenseType.OutOfBounds,
                Status = OffenseStatus.Cleared,
                DetectedAt = SeedData.BaseInstant.Plus(Duration.FromMinutes(15)),
                ClearedAt = SeedData.BaseInstant.Plus(Duration.FromMinutes(16)),
            });
            context.LocationLogs.Add(new LocationLog
            {
                MemberId = SeedData.HostMemberId,
                Timestamp = SeedData.BaseInstant.Plus(Duration.FromMinutes(5)),
                Latitude = 48.2,
                Longitude = 14.2,
                AccuracyMeters = 5,
                TransportMode = TransportMode.Foot,
                IsRevealedPosition = true,
            });

            return new ValueTask(context.SaveChangesAsync(TestCancellationToken));
        });

        GameResultsDto results = await GetResultsAsync(SeedData.SessionId);

        results.Timeline.Select(e => e.Type).Should().Contain(
            [TimelineEventType.MrXRevealed, TimelineEventType.PowerUpUsed, TimelineEventType.LeftGameArea]);
        results.Members.Single(m => m.MemberId == SeedData.HostMemberId).Stats.RevealCount.Should().Be(1);
        results.Members.Single(m => m.MemberId == SeedData.DetectiveMemberId).Stats.OutOfBoundsSeconds.Should().Be(60);
        results.Awards.Single(a => a.Type == AwardType.RuleBender).MemberIds.Should().Equal(SeedData.DetectiveMemberId);
    }
}
