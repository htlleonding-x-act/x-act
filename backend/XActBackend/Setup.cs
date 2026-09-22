using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using XActBackend.Core.Realtime;
using XActBackend.Core.Util;
using XActBackend.Persistence.Util;
using XActBackend.Realtime;
using XActBackend.Shared;
using XActBackend.Util;

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

        public void AddKeycloakAuthentication(IConfigurationManager configurationManager)
        {
            var settings = Activator.CreateInstance<AuthenticationSettings>();
            configurationManager.GetSection(AuthenticationSettings.SectionKey).Bind(settings);

            if (string.IsNullOrWhiteSpace(settings.Authority))
            {
                throw new InvalidOperationException("Authentication authority has to be configured");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = settings.Authority;
                        options.RequireHttpsMetadata = settings.RequireHttpsMetadata;

                        // keeps the raw jwt claim names instead of mapping them to the longer soap claim types
                        options.MapInboundClaims = false;

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = settings.ValidIssuer ?? settings.Authority,
                            // keycloak issues access tokens for the "account" audience unless the client gets an
                            // audience mapper, so there is nothing stable to validate against yet
                            ValidateAudience = false
                        };
                    });

            services.AddAuthorization();

            Log.Logger.Debug("Added keycloak authentication with authority {Authority}", settings.Authority);
        }

        public Settings LoadAndConfigureSettings(IConfigurationManager configurationManager)
        {
            var configSection = configurationManager.GetSection(Settings.SectionKey);

            services.Configure<Settings>(s => configSection.Bind(s));

            // a second copy with the same values for startup code that runs before dependency injection is set up
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
                // WithOrigins can't handle a wildcard port like http://localhost:*, so an origin ending in :*
                // allows any loopback origin instead
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
            services.AddSingleton<ILobbyDisconnectCleanup, LobbyDisconnectCleanup>();
        }
    }
}
