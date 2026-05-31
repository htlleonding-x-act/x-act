using NodaTime;

namespace XActBackend.Importer;

internal static class SeedData
{
    public const int SessionId = 1;
    public const int SessionTwoId = 2;
    public const string SessionJoinCode = "ALPHA1";
    public const string SessionTwoJoinCode = "BRAVO2";

    public const int HostUserId = 1;
    public const int DetectiveUserId = 2;
    public const int SpectatorUserId = 3;

    public const int MrXTeamId = 1;
    public const int DetectiveTeamId = 2;
    public const int SessionTwoTeamId = 3;

    public const int HostMemberId = 1;
    public const int DetectiveMemberId = 2;
    public const int GuestMemberId = 3;
    public const int SessionTwoMemberId = 4;

    public const int GeofencePointOneId = 1;
    public const int GeofencePointTwoId = 2;

    public const int LocationLogOneId = 1;
    public const int LocationLogTwoId = 2;

    public const int PowerUpUsageId = 1;

    public const int AllChatMessageId = 1;
    public const int TeamChatMessageId = 2;

    public static readonly Instant BaseInstant = Instant.FromUtc(2026, 1, 1, 10, 0);
}
