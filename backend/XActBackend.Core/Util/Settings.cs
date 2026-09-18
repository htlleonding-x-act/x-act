namespace XActBackend.Core.Util;

public sealed class Settings
{
    public const string SectionKey = "General";
    public required string ClientOrigin { get; init; }
    public TimeSpan LobbyDisconnectGracePeriod { get; init; } = TimeSpan.FromMinutes(1);
}
