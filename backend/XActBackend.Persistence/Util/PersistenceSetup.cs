using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;



namespace XActBackend.Persistence.Util;

public static class PersistenceSetup
{
    private const string ConnectionStringName = "Postgres";
    private const string MigrationHistoryTable = "__EFMigrationsHistory";

    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration,
                                          bool isDev)
    {
        string connectionString = configuration.GetConnectionString(ConnectionStringName)
                                  ?? throw new InvalidOperationException("Connection string not found");
        services.AddDbContext<DatabaseContext>(optionsBuilder =>
        {
            ConfigureDatabaseContextOptions(optionsBuilder, connectionString,
                                            isDev);
        });
    }

    public static void ConfigureDatabaseContextOptions(DbContextOptionsBuilder optionsBuilder, string connectionString,
                                                       bool sensitiveDataLogging)
    {
        optionsBuilder.UseNpgsql(connectionString,
                                 options => options
                                            .UseNodaTime()
                                            .MigrationsHistoryTable(MigrationHistoryTable,
                                                                    DatabaseContext.SchemaName))
                      .ConfigureWarnings(warnings =>
                                             warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

        if (sensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging()
                          .EnableDetailedErrors();
        }
    }

/// <summary>
    /// Führt automatisch alle ausstehenden EF Core Migrationen aus.
/// </summary>
    public static void ApplyMigrations(this IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<DatabaseContext>();
                
                if (context.Database.GetPendingMigrations().Any())
                {
                    Console.WriteLine("[Docker-Init] Ausstehende EF Core Migrationen wurden gefunden. Starte Update...");
                    context.Database.Migrate();
                    Console.WriteLine("[Docker-Init] Datenbank erfolgreich auf den neuesten Stand migriert!");
                }
                else
                {
                    Console.WriteLine("[Docker-Init] Datenbank ist bereits auf dem neuesten Stand.");
                }
            }
            catch (Exception ex)
            {
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("PersistenceSetup");
                logger.LogError(ex, "[Docker-Init] Kritischer Fehler beim automatischen Migrieren der Datenbank!");
                throw;
            }
        }
    }

    public static void AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Authentication:Authority"];
                options.RequireHttpsMetadata = configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", false);
                // ValidIssuer may differ from Authority when the backend reaches Keycloak via
                // a Docker-internal hostname (keycloak:8080) but tokens carry the external issuer
                // (localhost:8080). Fall back to Authority when not explicitly set.
                var validIssuer = configuration["Authentication:ValidIssuer"]
                                  ?? configuration["Authentication:Authority"];
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = validIssuer,
                    ValidateAudience = false
                };
            });

        services.AddAuthorization();
    }

    extension(IServiceCollection services)
    {
        public void ConfigurePersistence(IConfigurationManager configurationManager,
                                         bool isDev)
        {
            ConfigureDatabase(services, configurationManager, isDev);

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITransactionProvider, UnitOfWork>();
        }
    }
}
