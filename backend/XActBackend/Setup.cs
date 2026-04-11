using XActBackend.Core.Util;
using XActBackend.Persistence.Util;
using XActBackend.Realtime;
using XActBackend.Shared;
using XActBackend.Util;
using Serilog;

namespace XActBackend;

public static class Setup
{
    public const string CorsPolicyName = "DefaultCorsPolicy";

    extension(IServiceCollection services)
    {
        public void AddApplicationServices(IConfigurationManager configurationManager,
                                           bool isDev)
        {
            services.ConfigurePersistence(configurationManager, isDev);
            services.ConfigureCore();
        }

        public Settings LoadAndConfigureSettings(IConfigurationManager configurationManager)
        {
            var configSection = configurationManager.GetSection(Settings.SectionKey);

            services.Configure<Settings>(s => configSection.Bind(s));

            // different instance, but the same values - used for startup config outside of DI context
            var settings = Activator.CreateInstance<Settings>();
            configSection.Bind(settings);

            return settings;
        }
    }

    extension(WebApplicationBuilder builder)
    {
        public void AddLogging()
        {
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog((_, _, config) =>
            {
                config
                    .ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            });
        }
    }

    extension(IServiceCollection services)
    {
        public void AddCors(Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ClientOrigin))
            {
                throw new InvalidOperationException("Client origin has to be configured");
            }

            services.AddCors(o => o.AddPolicy(CorsPolicyName, builder =>
            {
                // wildcard port (e.g. http://localhost:*) is not supported by WithOrigins;
                // use a loopback predicate so any localhost port is accepted in development
                if (settings.ClientOrigin.EndsWith(":*"))
                {
                    builder.SetIsOriginAllowed(origin =>
                        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback);
                }
                else
                {
                    builder.WithOrigins(settings.ClientOrigin);
                }

                builder.AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
            }));

            Log.Logger.Debug("Added CORS policy with client origin {ClientOrigin}", settings.ClientOrigin);
        }

        public void ConfigureAdditionalRouteConstraints()
        {
            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add(nameof(LocalDate),
                                          typeof(LocalDateRouteConstraint));
            });
        }

        public void AddRealtime(bool isDev)
        {
            services.AddSignalR()
                    .AddJsonProtocol(o => JsonConfig.ConfigureJsonSerialization(o.PayloadSerializerOptions, isDev));

            services.AddScoped<IGameSessionRealtimePublisher, GameSessionRealtimePublisher>();
        }
    }
}
