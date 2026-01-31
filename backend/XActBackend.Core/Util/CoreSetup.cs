using XActBackend.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace XActBackend.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        
        services.AddScoped<IRocketService, RocketService>();
    }
}
