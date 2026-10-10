using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Repositories;
using XActBackend.Persistence.Util;

namespace XActBackend.Test;

public sealed class PlayerStatsServiceTests
{
    private const string UserId = "player";
    private const int MrXTeamId = 10;
    private const int DetectiveTeamId = 20;

    private static readonly Instant Start = Instant.FromUtc(2026, 1, 1, 12, 0);

    private readonly ITeamMemberRepository _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
    private readonly IGameResultsService _gameResultsService = Substitute.For<IGameResultsService>();
    private readonly PlayerStatsService _sut;

    public PlayerStatsServiceTests()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.TeamMemberRepository.Returns(_teamMemberRepository);
        _sut = new PlayerStatsService(uow, _gameResultsService, Substitute.For<ILogger<PlayerStatsService>>());
    }

    [Fact]
    public async ValueTask GetStatsAsync_SumsAllMatchesAndCountsWins()
    {
        GivenFinishedSessions(null, 1, 2);
        GivenResults(1, userTeamId: MrXTeamId, distance: 1200, mrXSeconds: 600, catches: 0, powerUps: 1, topSpeed: 9);
        GivenResults(2, userTeamId: DetectiveTeamId, distance: 800, mrXSeconds: 0, catches: 1, powerUps: 0, topSpeed: 14);

        IPlayerStatsService.PlayerStats stats = await _sut.GetStatsAsync(UserId);

        stats.GamesPlayed.Should().Be(2);
        stats.Wins.Should().Be(1);
        stats.DistanceMeters.Should().Be(2000);
        stats.MrXSeconds.Should().Be(600);
        stats.CatchesMade.Should().Be(1);
        stats.PowerUpsUsed.Should().Be(1);
        stats.TopSpeedKmh.Should().Be(14);
    }

    [Fact]
    public async ValueTask GetStatsAsync_ReturnsZeros_WhenUserHasNoMatches()
    {
        GivenFinishedSessions(null);

        IPlayerStatsService.PlayerStats stats = await _sut.GetStatsAsync(UserId);

        stats.Should().Be(new IPlayerStatsService.PlayerStats(0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public async ValueTask GetMatchHistoryAsync_ReturnsTheUsersViewOfEachMatch()
    {
        GivenFinishedSessions(5, 1);
        GivenResults(1, userTeamId: MrXTeamId, distance: 1200, mrXSeconds: 600, catches: 0, powerUps: 0, topSpeed: 9);

        IReadOnlyList<IPlayerStatsService.MatchSummary> matches = await _sut.GetMatchHistoryAsync(UserId, 5);

        matches.Should().ContainSingle();
        IPlayerStatsService.MatchSummary match = matches[0];
        match.SessionId.Should().Be(1);
        match.MemberId.Should().Be(100 + 1);
        match.TeamName.Should().Be("Mister X");
        match.Won.Should().BeTrue();
        match.Stats.DistanceMeters.Should().Be(1200);
    }

    [Fact]
    public async ValueTask GetMatchHistoryAsync_SkipsSessionsWithoutResults()
    {
        GivenFinishedSessions(20, 1, 2);
        GivenResults(1, userTeamId: DetectiveTeamId, distance: 0, mrXSeconds: 0, catches: 0, powerUps: 0, topSpeed: 0);
        _gameResultsService.GetResultsAsync(2).Returns(new NotFound());

        IReadOnlyList<IPlayerStatsService.MatchSummary> matches = await _sut.GetMatchHistoryAsync(UserId, 20);

        matches.Should().ContainSingle(m => m.SessionId == 1);
    }

    private void GivenFinishedSessions(int? limit, params int[] sessionIds)
    {
        _teamMemberRepository.GetFinishedSessionIdsOfUserAsync(UserId, limit).Returns(sessionIds);
    }

    private void GivenResults(int sessionId, int userTeamId, double distance, double mrXSeconds, int catches,
                              int powerUps, double topSpeed)
    {
        var session = new GameSession
        {
            Id = sessionId,
            HostUserId = UserId,
            SessionName = $"Match {sessionId}",
            JoinCode = $"CODE{sessionId}",
            Status = SessionStatus.Finished,
        };
        var mrXTeam = new Team { Id = MrXTeamId, SessionId = sessionId, TeamName = "Mister X", Role = TeamRole.MrX, ColorCode = "#EF4444" };
        var detectiveTeam = new Team { Id = DetectiveTeamId, SessionId = sessionId, TeamName = "Hunters", Role = TeamRole.Detective, ColorCode = "#5B7CFA" };
        var userMember = new TeamMember { Id = 100 + sessionId, SessionId = sessionId, TeamId = userTeamId, UserId = UserId };
        var otherMember = new TeamMember
        {
            Id = 200 + sessionId,
            SessionId = sessionId,
            TeamId = userTeamId == MrXTeamId ? DetectiveTeamId : MrXTeamId,
            GuestName = "Guest",
        };

        var results = new IGameResultsService.GameResults(
            session,
            Start,
            Start + Duration.FromHours(1),
            MrXTeamId,
            [TeamResult(mrXTeam), TeamResult(detectiveTeam)],
            [
                MemberResult(userMember, Stats(distance, mrXSeconds, catches, powerUps, topSpeed)),
                MemberResult(otherMember, Stats(5000, 0, 3, 3, 30)),
            ],
            [],
            [],
            [],
            []
        );

        _gameResultsService.GetResultsAsync(sessionId).Returns(results);
    }

    private static IGameResultsService.TeamResult TeamResult(Team team) =>
        new(team, null, 1, new IGameResultsService.TeamTotals(0, 0, 0, 0, 0, 0, 0));

    private static IGameResultsService.MemberResult MemberResult(TeamMember member, IGameResultsService.MemberStats stats) =>
        new(member, member.GuestName ?? member.UserId!, member.UserId is null, false, stats, []);

    private static IGameResultsService.MemberStats Stats(double distance, double mrXSeconds, int catches, int powerUps,
                                                         double topSpeed) =>
        new(distance, topSpeed, null, null, 0, new Dictionary<TransportMode, double>(), 0, 0, powerUps, mrXSeconds, 0,
            catches);
}
