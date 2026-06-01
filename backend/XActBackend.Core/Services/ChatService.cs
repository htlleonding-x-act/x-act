using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Provides methods to read and post chat messages for a game session.
///     Channels are implicit: <c>null</c> team id is the global "All" channel of the session,
///     a non-null team id is that team's private channel.
/// </summary>
public interface IChatService
{
    /// <summary>
    ///     The default maximum number of messages returned when loading a channel's history.
    /// </summary>
    public const int DefaultHistoryLimit = 100;

    /// <summary>
    ///     Get the recent messages of the global "All" channel of a session, oldest first.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <returns>The recent global messages, or not found if the session does not exist</returns>
    public ValueTask<OneOf<IReadOnlyCollection<ChatMessage>, NotFound>> GetSessionMessagesAsync(int sessionId, int limit);

    /// <summary>
    ///     Get the recent messages of a team channel, oldest first.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team channel</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <returns>The recent team messages, or not found if the team does not belong to the session</returns>
    public ValueTask<OneOf<IReadOnlyCollection<ChatMessage>, NotFound>> GetTeamMessagesAsync(int sessionId, int teamId, int limit);

    /// <summary>
    ///     Post a message to the global "All" channel of a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="senderMemberId">The id of the sending member</param>
    /// <param name="content">The message content</param>
    /// <returns>The created message, not found or a domain error if validation fails</returns>
    public ValueTask<OneOf<ChatMessage, NotFound, DomainError>> PostSessionMessageAsync(int sessionId, int senderMemberId, string content);

    /// <summary>
    ///     Post a message to a team's private channel.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team channel</param>
    /// <param name="senderMemberId">The id of the sending member</param>
    /// <param name="content">The message content</param>
    /// <returns>The created message, not found or a domain error if validation fails</returns>
    public ValueTask<OneOf<ChatMessage, NotFound, DomainError>> PostTeamMessageAsync(int sessionId, int teamId, int senderMemberId, string content);
}

internal sealed class ChatService(IUnitOfWork uow, IClock clock, ILogger<ChatService> logger) : IChatService
{
    public async ValueTask<OneOf<IReadOnlyCollection<ChatMessage>, NotFound>> GetSessionMessagesAsync(int sessionId, int limit)
    {
        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            return new NotFound();
        }

        IReadOnlyCollection<ChatMessage> messages = await uow.ChatMessageRepository.GetSessionMessagesAsync(sessionId, NormalizeLimit(limit), tracking: false);
        return OneOf<IReadOnlyCollection<ChatMessage>, NotFound>.FromT0(messages);
    }

    public async ValueTask<OneOf<IReadOnlyCollection<ChatMessage>, NotFound>> GetTeamMessagesAsync(int sessionId, int teamId, int limit)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking: false);
        if (team is null || team.SessionId != sessionId)
        {
            return new NotFound();
        }

        IReadOnlyCollection<ChatMessage> messages = await uow.ChatMessageRepository.GetTeamMessagesAsync(sessionId, teamId, NormalizeLimit(limit), tracking: false);
        return OneOf<IReadOnlyCollection<ChatMessage>, NotFound>.FromT0(messages);
    }

    public async ValueTask<OneOf<ChatMessage, NotFound, DomainError>> PostSessionMessageAsync(int sessionId, int senderMemberId, string content)
    {
        OneOf<TeamMember, NotFound> senderResult = await ResolveSenderAsync(sessionId, senderMemberId);
        if (senderResult.TryPickT1(out NotFound notFound, out TeamMember sender))
        {
            return notFound;
        }

        return await CreateMessageAsync(sessionId, teamId: null, sender, content);
    }

    public async ValueTask<OneOf<ChatMessage, NotFound, DomainError>> PostTeamMessageAsync(int sessionId, int teamId, int senderMemberId, string content)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking: false);
        if (team is null || team.SessionId != sessionId)
        {
            logger.LogWarning("Rejected team chat message because team {TeamId} does not belong to session {SessionId}", teamId, sessionId);
            return new NotFound();
        }

        OneOf<TeamMember, NotFound> senderResult = await ResolveSenderAsync(sessionId, senderMemberId);
        if (senderResult.TryPickT1(out NotFound notFound, out TeamMember sender))
        {
            return notFound;
        }

        // Access control: a member can only post to the channel of the team they belong to.
        if (sender.TeamId != teamId)
        {
            logger.LogWarning("Rejected team chat message because member {MemberId} is not part of team {TeamId}", senderMemberId, teamId);
            return DomainError.ChatNotTeamMember(senderMemberId, teamId);
        }

        return await CreateMessageAsync(sessionId, teamId, sender, content);
    }

    private async ValueTask<OneOf<TeamMember, NotFound>> ResolveSenderAsync(int sessionId, int senderMemberId)
    {
        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            logger.LogWarning("Rejected chat message because session {SessionId} does not exist", sessionId);
            return new NotFound();
        }

        var sender = await uow.TeamMemberRepository.GetMemberByIdAsync(senderMemberId, tracking: false);
        if (sender is null || sender.SessionId != sessionId)
        {
            logger.LogWarning("Rejected chat message because member {MemberId} is not part of session {SessionId}", senderMemberId, sessionId);
            return new NotFound();
        }

        return sender;
    }

    private async ValueTask<ChatMessage> CreateMessageAsync(int sessionId, int? teamId, TeamMember sender, string content)
    {
        string senderName = await ResolveSenderNameAsync(sender);

        var message = uow.ChatMessageRepository.AddChatMessage(
            sessionId,
            teamId,
            sender.Id,
            sender.TeamId,
            senderName,
            content.Trim(),
            clock.GetCurrentInstant());

        await uow.SaveChangesAsync();

        logger.LogInformation(
            "Created chat message {MessageId} in session {SessionId} (team {TeamId}) from member {MemberId}",
            message.Id, sessionId, teamId, sender.Id);

        return message;
    }

    private async ValueTask<string> ResolveSenderNameAsync(TeamMember sender)
    {
        if (!string.IsNullOrWhiteSpace(sender.UserId))
        {
            var user = await uow.UserRepository.GetUserByIdAsync(sender.UserId, tracking: false);
            if (!string.IsNullOrWhiteSpace(user?.Username))
            {
                return Truncate(user.Username);
            }
        }

        if (!string.IsNullOrWhiteSpace(sender.GuestName))
        {
            return Truncate(sender.GuestName);
        }

        return "Unknown";
    }

    private static string Truncate(string value) =>
        value.Length <= ChatMessage.MaxSenderNameLength ? value : value[..ChatMessage.MaxSenderNameLength];

    private static int NormalizeLimit(int limit) =>
        limit <= 0 ? IChatService.DefaultHistoryLimit : Math.Min(limit, IChatService.DefaultHistoryLimit);
}
