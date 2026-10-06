using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Repositories;
using XActBackend.Persistence.Util;

namespace XActBackend.Test;

public sealed class GameResultsServiceTests
{
    private const int SessionId = 1;
    private const string HostUserId = "host";
    private const int MrXTeamId = 10;
    private const int DetectiveTeamId = 20;
    private const int SpectatorTeamId = 30;

    private static readonly Instant Start = Instant.FromUtc(2026, 1, 1, 12, 0);
    private static readonly Instant End = Start + Duration.FromHours(1);

    private readonly IGameSessionRepository _gameSessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly ITeamMemberRepository _teamMemberRepository = Substitute.For<ITeamMemberRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILocationLogRepository _locationLogRepository = Substitute.For<ILocationLogRepository>();
    private readonly ICatchEventRepository _catchEventRepository = Substitute.For<ICatchEventRepository>();
    private readonly IOffenseRepository _offenseRepository = Substitute.For<IOffenseRepository>();
    private readonly IPowerUpUsageRepository _powerUpUsageRepository = Substitute.For<IPowerUpUsageRepository>();
    private readonly IGeofencePointRepository _geofencePointRepository = Substitute.For<IGeofencePointRepository>();
    private readonly GameResultsService _sut;

    private readonly GameSession _session = new()
    {
        Id = SessionId,
        HostUserId = HostUserId,
        SessionName = "Match",
        JoinCode = "ABC123",
        Status = SessionStatus.Finished,
        StartTime = Start,
        EndTime = End,
        MrXRevealInterval = 5,
    };

    private List<Team> _teams =
    [
        new() { Id = MrXTeamId, SessionId = SessionId, TeamName = "Team 1", Role = TeamRole.MrX, ColorCode = "#EF4444" },
        new() { Id = DetectiveTeamId, SessionId = SessionId, TeamName = "Team 2", Role = TeamRole.Detective, ColorCode = "#5B7CFA" },
    ];

    private List<TeamMember> _members =
    [
        new() { Id = 1, SessionId = SessionId, TeamId = MrXTeamId, UserId = HostUserId },
        new() { Id = 2, SessionId = SessionId, TeamId = DetectiveTeamId, GuestName = "Guest A" },
    ];

    private List<LocationLog> _logs = [];
    private List<CatchEvent> _catches = [];

    public GameResultsServiceTests()
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.GameSessionRepository.Returns(_gameSessionRepository);
        uow.TeamRepository.Returns(_teamRepository);
        uow.TeamMemberRepository.Returns(_teamMemberRepository);
        uow.UserRepository.Returns(_userRepository);
        uow.LocationLogRepository.Returns(_locationLogRepository);
        uow.CatchEventRepository.Returns(_catchEventRepository);
        uow.OffenseRepository.Returns(_offenseRepository);
        uow.PowerUpUsageRepository.Returns(_powerUpUsageRepository);
        uow.GeofencePointRepository.Returns(_geofencePointRepository);

        _gameSessionRepository.GetSessionByIdAsync(SessionId, false).Returns(_session);
        _teamRepository.GetTeamsBySessionIdAsync(SessionId, false).Returns(_ => _teams);
        _teamMemberRepository.GetMembersBySessionIdAsync(SessionId, false).Returns(_ => _members);
        _userRepository.GetUsersByIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), false)
            .Returns([new User { Id = HostUserId, Username = "host_user" }]);
        _locationLogRepository.GetLogsBySessionIdAsync(SessionId, false).Returns(_ => _logs);
        _catchEventRepository.GetCatchEventsBySessionIdAsync(SessionId, false).Returns(_ => _catches);
        _offenseRepository.GetOffensesBySessionAsync(SessionId, false).Returns([]);
        _powerUpUsageRepository.GetUsagesBySessionIdAsync(SessionId, false).Returns([]);
        _geofencePointRepository.GetPointsBySessionIdAsync(SessionId, false).Returns([]);

        _sut = new GameResultsService(uow, Substitute.For<ILogger<GameResultsService>>());
    }

    private static LocationLog Log(int memberId, int seconds, double latitude, double longitude = 14.3) =>
        new()
        {
            MemberId = memberId,
            Timestamp = Start + Duration.FromSeconds(seconds),
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = 5,
            TransportMode = TransportMode.Foot,
        };

    /// <summary>a walk north of 11 m every 5 s</summary>
    private static IEnumerable<LocationLog> Walk(int memberId, int count, double startLatitude = 48.3, double longitude = 14.3) =>
        Enumerable.Range(0, count).Select(i => Log(memberId, i * 5, startLatitude + i * 0.0001, longitude));

    private async ValueTask<IGameResultsService.GameResults> GetResultsAsync()
    {
        OneOf<IGameResultsService.GameResults, NotFound, DomainError> result = await _sut.GetResultsAsync(SessionId);

        return result.Match(
            results => results,
            _ => throw new InvalidOperationException("Expected results but got NotFound"),
            error => throw new InvalidOperationException($"Expected results but got DomainError {error.Code}"));
    }

    [Fact]
    public async ValueTask GetResultsAsync_ReturnsNotFound_WhenSessionMissing()
    {
        OneOf<IGameResultsService.GameResults, NotFound, DomainError> result = await _sut.GetResultsAsync(99);

        result.Switch(
            _ => Assert.Fail("Expected NotFound but got results"),
            _ => { /* expected */ },
            _ => Assert.Fail("Expected NotFound but got DomainError"));
    }

    [Fact]
    public async ValueTask GetResultsAsync_ReturnsDomainError_WhenSessionNotFinished()
    {
        _session.Status = SessionStatus.Active;

        OneOf<IGameResultsService.GameResults, NotFound, DomainError> result = await _sut.GetResultsAsync(SessionId);

        result.Switch(
            _ => Assert.Fail("Expected DomainError but got results"),
            _ => Assert.Fail("Expected DomainError but got NotFound"),
            error => error.Code.Should().Be(DomainErrorCodes.SessionNotFinished));
    }

    [Fact]
    public async ValueTask GetResultsAsync_ResolvesUserAndGuestDisplayNames()
    {
        IGameResultsService.GameResults results = await GetResultsAsync();

        IGameResultsService.MemberResult host = results.Members.Single(m => m.Member.Id == 1);
        IGameResultsService.MemberResult guest = results.Members.Single(m => m.Member.Id == 2);
        host.DisplayName.Should().Be("host_user");
        host.IsHost.Should().BeTrue();
        host.IsGuest.Should().BeFalse();
        guest.DisplayName.Should().Be("Guest A");
        guest.IsGuest.Should().BeTrue();
    }

    [Fact]
    public async ValueTask GetResultsAsync_PicksFinalMrXTeamWithMembersAsWinner() =>
        (await GetResultsAsync()).WinnerTeamId.Should().Be(MrXTeamId);

    [Fact]
    public async ValueTask GetResultsAsync_ReturnsNoWinner_WhenMrXTeamIsEmpty()
    {
        _members = _members.Where(m => m.TeamId != MrXTeamId).ToList();

        (await GetResultsAsync()).WinnerTeamId.Should().BeNull();
    }

    [Fact]
    public async ValueTask GetResultsAsync_ComputesDistanceAndRoute()
    {
        _logs = Walk(2, 100).ToList();

        IGameResultsService.MemberResult detective = (await GetResultsAsync()).Members.Single(m => m.Member.Id == 2);

        detective.Stats.DistanceMeters.Should().BeApproximately(99 * 11.12, 10);
        detective.Route.Should().NotBeEmpty();
    }

    [Fact]
    public async ValueTask GetResultsAsync_ExcludesSpectatorsFromAwards()
    {
        _teams.Add(new Team { Id = SpectatorTeamId, SessionId = SessionId, TeamName = "Unassigned", Role = TeamRole.Spectator, ColorCode = "#64748B" });
        _members.Add(new TeamMember { Id = 3, SessionId = SessionId, TeamId = SpectatorTeamId, GuestName = "Watcher" });
        _logs = [.. Walk(3, 200), .. Walk(2, 100)];

        IGameResultsService.GameResults results = await GetResultsAsync();

        results.Awards.Single(a => a.Type == AwardType.Marathon).MemberIds.Should().Equal(2);
    }

    [Fact]
    public async ValueTask GetResultsAsync_CreditsCatchToClosestCatchingMember()
    {
        _members.Add(new TeamMember { Id = 4, SessionId = SessionId, TeamId = DetectiveTeamId, GuestName = "Guest B" });
        // mr.x stands still, guest b right next to him and guest a 1 km away
        _logs = [Log(1, 0, 48.3), Log(1, 60, 48.3), Log(2, 0, 48.31), Log(2, 60, 48.31), Log(4, 0, 48.3001), Log(4, 60, 48.3001)];
        _catches = [new CatchEvent { SessionId = SessionId, OccurredAt = Start + Duration.FromSeconds(30), CatchingTeamId = DetectiveTeamId, CaughtTeamId = MrXTeamId }];
        _teams[0].Role = TeamRole.Detective;
        _teams[1].Role = TeamRole.MrX;

        IGameResultsService.GameResults results = await GetResultsAsync();

        results.Members.Single(m => m.Member.Id == 4).Stats.CatchesMade.Should().Be(1);
        results.Members.Single(m => m.Member.Id == 2).Stats.CatchesMade.Should().Be(0);
        results.Timeline.Single(e => e.Type == TimelineEventType.MrXCaught).MemberId.Should().Be(4);
        results.MrXPeriods.Select(p => p.TeamId).Should().Equal(MrXTeamId, DetectiveTeamId);
    }

    [Fact]
    public async ValueTask GetResultsAsync_FallsBackToLogTimes_WhenStartTimeMissing()
    {
        _session.StartTime = null;
        _session.EndTime = null;
        _logs = Walk(2, 10).ToList();

        IGameResultsService.GameResults results = await GetResultsAsync();

        results.Start.Should().Be(Start);
        results.End.Should().Be(Start + Duration.FromSeconds(45));
    }
}
