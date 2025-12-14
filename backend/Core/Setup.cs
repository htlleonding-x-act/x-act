using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime.Serialization.SystemTextJson;
using XAct.Core.GameSessions;
using XAct.Core.GeofencePoints;
using XAct.Core.LocationLogs;
using XAct.Core.PowerUpUsages;
using XAct.Core.TeamMembers;
using XAct.Core.Teams;
using XAct.Core.Users;

namespace XAct.Core;

public static class Setup
{    
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton<IDataStorage, DataStorage>();
        
        services.AddScoped<IGameSessionService, GameSessionService>();
        services.AddScoped<IGeofencePointService, GeofencePointService>();
        services.AddScoped<ILocationLogService, LocationLogService>();
        services.AddScoped<IPowerUpUsageService, PowerUpUsageService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IUserService, UserService>();
    }

    public static void ConfigureServices(this IServiceCollection services, bool isDevelopment)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = isDevelopment;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        });
    }
    
    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(Const.CorsPolicyName, policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }
}
