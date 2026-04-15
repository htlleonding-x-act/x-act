namespace XActBackend.Persistence.Model;

public class Team
{
    public const int DefaultMaxPlayerCount = 6;

    public int Id { get; set; }

    public int SessionId { get; set; }

    public required string TeamName { get; set; }

    public TeamRole Role { get; set; }

    public required string ColorCode { get; set; }

    public int MaxPlayerCount { get; set; } = DefaultMaxPlayerCount;

    public bool IsCaught { get; set; }


    public GameSession Session { get; set; } = null!;

    public ICollection<TeamMember> Members { get; set; } = [];
}
