using Microsoft.Extensions.DependencyInjection;
using XActBackend.Core.Services;

namespace XActBackend.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<IGeofencePointService, GeoFencePointService>();
        services.AddScoped<ILocationLogService, LocationLogService>();
        services.AddScoped<IPowerUpUsageService, PowerUpUsageService>();
    }
}
