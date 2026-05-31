using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions/{sessionId:int}/chat")]
public sealed class ChatController(
    ITransactionProvider transaction,
    IChatService chatService,
    IGameSessionRealtimePublisher realtimePublisher,
    ILogger<ChatController> logger) : BaseController
{
    [HttpGet]
    [Route("all")]
    [ProducesResponseType<ChatMessageListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ChatMessageListResponse>> GetAllMessages(
        [FromRoute] int sessionId,
        [FromQuery] int limit = IChatService.DefaultHistoryLimit)
    {
        OneOf<IReadOnlyCollection<ChatMessage>, NotFound> result = await chatService.GetSessionMessagesAsync(sessionId, limit);

        return result.Match<ActionResult<ChatMessageListResponse>>(
            messages => Ok(ToListResponse(messages)),
            notFound => NotFound());
    }

    [HttpGet]
    [Route("teams/{teamId:int}")]
    [ProducesResponseType<ChatMessageListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ChatMessageListResponse>> GetTeamMessages(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromQuery] int limit = IChatService.DefaultHistoryLimit)
    {
        OneOf<IReadOnlyCollection<ChatMessage>, NotFound> result = await chatService.GetTeamMessagesAsync(sessionId, teamId, limit);

        return result.Match<ActionResult<ChatMessageListResponse>>(
            messages => Ok(ToListResponse(messages)),
            notFound => NotFound());
    }

    [HttpPost]
    [Route("all")]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<IActionResult> PostAllMessage(
        [FromRoute] int sessionId,
        [FromBody] ChatMessagePostRequest request) =>
        PostMessageAsync(
            sessionId,
            request,
            () => chatService.PostSessionMessageAsync(sessionId, request.SenderMemberId, request.Content));

    [HttpPost]
    [Route("teams/{teamId:int}")]
    [ProducesResponseType<ChatMessageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ValueTask<IActionResult> PostTeamMessage(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromBody] ChatMessagePostRequest request) =>
        PostMessageAsync(
            sessionId,
            request,
            () => chatService.PostTeamMessageAsync(sessionId, teamId, request.SenderMemberId, request.Content));

    private async ValueTask<IActionResult> PostMessageAsync(
        int sessionId,
        ChatMessagePostRequest request,
        Func<ValueTask<OneOf<ChatMessage, NotFound, DomainError>>> post)
    {
        if (!ValidateRequest<ChatMessagePostRequest.Validator, ChatMessagePostRequest>(request))
        {
            logger.LogWarning("Rejected chat message in session {SessionId} because validation failed", sessionId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<ChatMessage, NotFound, DomainError> postResult = await post();

            return await postResult.Match<ValueTask<IActionResult>>(async message =>
            {
                await transaction.CommitAsync();
                await realtimePublisher.PublishChatMessageAsync(message);
                logger.LogInformation("Created chat message {MessageId} in session {SessionId}", message.Id, sessionId);

                return StatusCode(StatusCodes.Status201Created, ChatMessageDto.FromMessage(message));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected chat message in session {SessionId} because the session, team or sender was not found", sessionId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected chat message in session {SessionId} with domain error {ErrorCode}: {ErrorMessage}", sessionId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post chat message in session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    private static ChatMessageListResponse ToListResponse(IReadOnlyCollection<ChatMessage> messages) =>
        new() { Items = messages.Select(ChatMessageDto.FromMessage).ToList() };
}

public sealed class ChatMessageListResponse
{
    public required List<ChatMessageDto> Items { get; init; }
}

public sealed record ChatMessageDto(
    int Id,
    int SessionId,
    int? TeamId,
    int? SenderMemberId,
    int? SenderTeamId,
    string SenderName,
    string Content,
    Instant SentAt
)
{
    public static ChatMessageDto FromMessage(ChatMessage message) =>
        new(
            message.Id,
            message.SessionId,
            message.TeamId,
            message.SenderMemberId,
            message.SenderTeamId,
            message.SenderName,
            message.Content,
            message.SentAt
        );
}

public sealed record ChatMessagePostRequest(
    int SenderMemberId,
    string Content
)
{
    public sealed class Validator : AbstractValidator<ChatMessagePostRequest>
    {
        public Validator()
        {
            RuleFor(x => x.SenderMemberId).GreaterThan(0);
            RuleFor(x => x.Content)
                .NotEmpty()
                .Must(content => !string.IsNullOrWhiteSpace(content))
                .WithMessage("Content must not be empty.")
                .MaximumLength(ChatMessage.MaxContentLength);
        }
    }
}
