namespace XActBackend.Core.Util;

public sealed class Settings
{
    public const string SectionKey = "General";
    public required string ClientOrigin { get; init; }
    // a phone locked in a pocket can lose its connection for a while, so a
    // waiting player is only removed after a long silence
    public TimeSpan LobbyDisconnectGracePeriod { get; init; } = TimeSpan.FromMinutes(15);
    // a match ends at most this long after its planned end
    public TimeSpan MatchTimeLimitCheckInterval { get; init; } = TimeSpan.FromSeconds(10);
}
