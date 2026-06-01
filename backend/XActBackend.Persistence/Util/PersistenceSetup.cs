using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


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
