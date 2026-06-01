using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     there are no channel rows: a null team id means the all chat of the session, any other team id is that
///     team's private chat
/// </summary>
public interface IChatService
{
    /// <summary>also the upper bound, a bigger limit gets clamped to it</summary>
    public const int DefaultHistoryLimit = 100;

    public ValueTask<OneOf<IReadOnlyCollection<ChatMessage>, NotFound>> GetSessionMessagesAsync(int sessionId, int limit);

    public ValueTask<OneOf<IReadOnlyCollection<ChatMessage>, NotFound>> GetTeamMessagesAsync(int sessionId, int teamId, int limit);

    public ValueTask<OneOf<ChatMessage, NotFound, DomainError>> PostSessionMessageAsync(int sessionId, int senderMemberId, string content);

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

        return await senderResult.Match<ValueTask<OneOf<ChatMessage, NotFound, DomainError>>>(
            async sender => await CreateMessageAsync(sessionId, teamId: null, sender, content),
            notFound => ValueTask.FromResult<OneOf<ChatMessage, NotFound, DomainError>>(notFound));
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

        return await senderResult.Match<ValueTask<OneOf<ChatMessage, NotFound, DomainError>>>(
            async sender =>
            {
                if (sender.TeamId != teamId)
                {
                    logger.LogWarning("Rejected team chat message because member {MemberId} is not part of team {TeamId}", senderMemberId, teamId);
                    return DomainError.ChatNotTeamMember(senderMemberId, teamId);
                }

                return await CreateMessageAsync(sessionId, teamId, sender, content);
            },
            notFound => ValueTask.FromResult<OneOf<ChatMessage, NotFound, DomainError>>(notFound));
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
