using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="ChatMessage"/> entities.
/// </summary>
public interface IChatMessageRepository
{
    /// <summary>
    ///     Add a new chat message.
    /// </summary>
    /// <param name="sessionId">The id of the session the message belongs to</param>
    /// <param name="teamId">The team channel id, or <c>null</c> for the global "All" channel</param>
    /// <param name="senderMemberId">The id of the sending member</param>
    /// <param name="senderTeamId">The sender's team at send time</param>
    /// <param name="senderName">The sender's display name captured at send time</param>
    /// <param name="content">The message content</param>
    /// <param name="sentAt">Timestamp the message was sent</param>
    /// <returns>The created chat message entity</returns>
    public ChatMessage AddChatMessage(
        int sessionId,
        int? teamId,
        int senderMemberId,
        int? senderTeamId,
        string senderName,
        string content,
        Instant sentAt
    );

    /// <summary>
    ///     Get the most recent messages of the global "All" channel of a session, oldest first.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>The most recent global messages, ordered oldest to newest</returns>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetSessionMessagesAsync(int sessionId, int limit, bool tracking);

    /// <summary>
    ///     Get the most recent messages of a team channel, oldest first.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team channel</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>The most recent team messages, ordered oldest to newest</returns>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetTeamMessagesAsync(int sessionId, int teamId, int limit, bool tracking);
}

internal sealed class ChatMessageRepository(DbSet<ChatMessage> messageSet) : IChatMessageRepository
{
    private IQueryable<ChatMessage> Messages => messageSet;
    private IQueryable<ChatMessage> MessagesNoTracking => Messages.AsNoTracking();

    public ChatMessage AddChatMessage(
        int sessionId,
        int? teamId,
        int senderMemberId,
        int? senderTeamId,
        string senderName,
        string content,
        Instant sentAt
    )
    {
        var message = new ChatMessage
        {
            SessionId = sessionId,
            TeamId = teamId,
            SenderMemberId = senderMemberId,
            SenderTeamId = senderTeamId,
            SenderName = senderName,
            Content = content,
            SentAt = sentAt,
        };

        messageSet.Add(message);

        return message;
    }

    public async ValueTask<IReadOnlyCollection<ChatMessage>> GetSessionMessagesAsync(int sessionId, int limit, bool tracking)
    {
        IQueryable<ChatMessage> source = tracking ? Messages : MessagesNoTracking;

        List<ChatMessage> messages = await source
            .Where(m => m.SessionId == sessionId && m.TeamId == null)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async ValueTask<IReadOnlyCollection<ChatMessage>> GetTeamMessagesAsync(int sessionId, int teamId, int limit, bool tracking)
    {
        IQueryable<ChatMessage> source = tracking ? Messages : MessagesNoTracking;

        List<ChatMessage> messages = await source
            .Where(m => m.SessionId == sessionId && m.TeamId == teamId)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();
        return messages;
    }
}
