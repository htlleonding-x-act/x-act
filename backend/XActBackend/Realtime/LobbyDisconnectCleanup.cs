using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Realtime;

public sealed record MemberPresenceRegistration(
    int SessionId,
    int TeamId,
    int MemberId,
    int? UserId,
    string? GuestName);

/// <summary>
///     Deletes the team member of a lobby player whose realtime connection dropped, after a grace period and
///     only if the session is still waiting. A locked phone or a quick app switch also drops the connection,
///     and the app does not rejoin on its own, so deleting right away would leave the player without a team.
/// </summary>
public interface ILobbyDisconnectCleanup
{
    void ScheduleRemoval(MemberPresenceRegistration registration);
}

internal sealed class LobbyDisconnectCleanup(
    IServiceScopeFactory scopeFactory,
    IOptions<Settings> settings,
    ILogger<LobbyDisconnectCleanup> logger) : ILobbyDisconnectCleanup
{
    public void ScheduleRemoval(MemberPresenceRegistration registration) =>
        _ = RemoveAfterGracePeriodAsync(registration);

    private async Task RemoveAfterGracePeriodAsync(MemberPresenceRegistration registration)
    {
        await Task.Delay(settings.Value.LobbyDisconnectGracePeriod);

        // The app reconnected within the grace period and registered this member again, so they stay on the team.
        if (GameSessionHub.GetConnectedMemberIds(registration.SessionId).Contains(registration.MemberId))
        {
            return;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var transaction = scope.ServiceProvider.GetRequiredService<ITransactionProvider>();
        var gameSessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
        var teamMemberService = scope.ServiceProvider.GetRequiredService<ITeamMemberService>();
        var realtimePublisher = scope.ServiceProvider.GetRequiredService<IGameSessionRealtimePublisher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        try
        {
            OneOf<GameSession, NotFound> sessionResult = await gameSessionService.GetGameSessionByIdAsync(registration.SessionId, tracking: false);

            bool isWaiting = sessionResult.Match(
                session => session.Status == SessionStatus.Waiting,
                _ => false);

            if (!isWaiting)
            {
                return;
            }

            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await teamMemberService.DeleteTeamMemberAsync(
                registration.SessionId,
                registration.TeamId,
                registration.MemberId,
                tracking: true);

            await deleteResult.Match(
                async _ =>
                {
                    await transaction.CommitAsync();
                    await realtimePublisher.PublishTeamMemberLeftAsync(
                        registration.SessionId,
                        registration.TeamId,
                        registration.MemberId,
                        registration.UserId,
                        registration.GuestName,
                        clock.GetCurrentInstant());
                },
                async _ => { await transaction.RollbackAsync(); });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed disconnect cleanup for member {MemberId} in session {SessionId}",
                              registration.MemberId, registration.SessionId);
            await transaction.RollbackAsync();
        }
    }
}
