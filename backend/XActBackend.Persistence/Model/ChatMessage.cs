namespace XActBackend.Persistence.Model;

public sealed class ChatMessage
{
    public const int MaxContentLength = 1000;
    public const int MaxSenderNameLength = 64;

    public int Id { get; set; }

    public int SessionId { get; set; }

    /// <summary>null means the session wide all chat</summary>
    public int? TeamId { get; set; }

    /// <summary>
    ///     nullable so the chat history survives the sender leaving, the reference is set to null when the
    ///     member is deleted
    /// </summary>
    public int? SenderMemberId { get; set; }

    /// <summary>
    ///     copied at send time so the all chat can still color a message after the sender has left
    /// </summary>
    public int? SenderTeamId { get; set; }

    /// <summary>username or guest name, copied at send time</summary>
    public required string SenderName { get; set; }

    public required string Content { get; set; }

    public Instant SentAt { get; set; }


    public GameSession Session { get; set; } = null!;

    public Team? Team { get; set; }

    public TeamMember? Sender { get; set; }
}
