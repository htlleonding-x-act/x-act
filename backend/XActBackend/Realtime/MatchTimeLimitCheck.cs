using Microsoft.Extensions.Options;
using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Realtime;

/// <summary>
///     ends running matches once their planned duration has passed, so a match ends on time even when no app is
///     open to notice it
/// </summary>
internal sealed class MatchTimeLimitCheck(
    IServiceScopeFactory scopeFactory,
    IOptions<Settings> settings,
    ILogger<MatchTimeLimitCheck> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(settings.Value.MatchTimeLimitCheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EndTimedOutMatchesAsync();
        }
    }

    private async Task EndTimedOutMatchesAsync()
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var transaction = scope.ServiceProvider.GetRequiredService<ITransactionProvider>();
        var gameSessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
        var realtimePublisher = scope.ServiceProvider.GetRequiredService<IGameSessionRealtimePublisher>();

        try
        {
            await transaction.BeginTransactionAsync();
            IReadOnlyCollection<GameSession> ended = await gameSessionService.EndTimedOutSessionsAsync();
            await transaction.CommitAsync();

            foreach (var session in ended)
            {
                await realtimePublisher.PublishGameSessionEndedAsync(session);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to end timed out matches");
            await transaction.RollbackAsync();
        }
    }
}
