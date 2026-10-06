using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Persistence.Util;

namespace XActBackend.Realtime;

/// <summary>
///     clears sessions left behind by crashed or killed apps every few minutes, so they don't stay open forever and
///     players still waiting in such a lobby get told it is gone
/// </summary>
internal sealed class AbandonedSessionCleanup(
    IServiceScopeFactory scopeFactory,
    ILogger<AbandonedSessionCleanup> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    // the host joins right after creating the lobby, a young session isn't abandoned yet
    private static readonly Duration MinimumAge = Duration.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanUpOnceAsync();
        }
    }

    private async Task CleanUpOnceAsync()
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var transaction = scope.ServiceProvider.GetRequiredService<ITransactionProvider>();
        var gameSessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
        var realtimePublisher = scope.ServiceProvider.GetRequiredService<IGameSessionRealtimePublisher>();

        try
        {
            await transaction.BeginTransactionAsync();
            IGameSessionService.AbandonedSessions result = await gameSessionService.CleanUpAbandonedSessionsAsync(MinimumAge);
            await transaction.CommitAsync();

            foreach (int sessionId in result.DeletedSessionIds)
            {
                await realtimePublisher.PublishGameSessionDeletedAsync(sessionId);
            }

            foreach (var session in result.FinishedSessions)
            {
                await realtimePublisher.PublishGameSessionEndedAsync(session);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up abandoned sessions");
            await transaction.RollbackAsync();
        }
    }
}
