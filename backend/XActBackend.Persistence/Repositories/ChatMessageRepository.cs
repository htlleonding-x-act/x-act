using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IChatMessageRepository
{
    public ChatMessage AddChatMessage(
        int sessionId,
        int? teamId,
        int senderMemberId,
        int? senderTeamId,
        string senderName,
        string content,
        Instant sentAt
    );

    /// <summary>the newest <paramref name="limit"/> messages of the all chat, returned oldest first</summary>
    public ValueTask<IReadOnlyCollection<ChatMessage>> GetSessionMessagesAsync(int sessionId, int limit, bool tracking);

    /// <summary>the newest <paramref name="limit"/> messages of the team chat, returned oldest first</summary>
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
