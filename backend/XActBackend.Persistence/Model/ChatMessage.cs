namespace XActBackend.Persistence.Model;

public class ChatMessage
{
    public const int MaxContentLength = 1000;
    public const int MaxSenderNameLength = 64;

    public int Id { get; set; }

    public int SessionId { get; set; }

    /// <summary>
    ///     The team this message belongs to. <c>null</c> denotes the global "All" channel
    ///     visible to every member of the session.
    /// </summary>
    public int? TeamId { get; set; }

    /// <summary>
    ///     The member that sent the message. Nullable so chat history survives a member
    ///     leaving the lobby (the reference is set to null on member deletion).
    /// </summary>
    public int? SenderMemberId { get; set; }

    /// <summary>
    ///     The sender's team at the time the message was sent. Stored denormalized so the
    ///     "All" channel can attribute/colour a message even after the sender has left.
    /// </summary>
    public int? SenderTeamId { get; set; }

    /// <summary>
    ///     The sender's display name (username or guest name) captured at send time.
    /// </summary>
    public required string SenderName { get; set; }

    public required string Content { get; set; }

    public Instant SentAt { get; set; }


    public GameSession Session { get; set; } = null!;

    public Team? Team { get; set; }

    public TeamMember? Sender { get; set; }
}
