using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    extension(IServiceProvider serviceProvider)
    {
        /// <summary>applies pending migrations so a fresh development database is usable right after startup</summary>
        public void ApplyMigrations()
        {
            using var scope = serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseContext>>();

            var pendingMigrations = context.Database.GetPendingMigrations().ToList();

            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation("Applying {MigrationCount} pending migrations", pendingMigrations.Count);
                context.Database.Migrate();
            }
        }
    }

    extension(IServiceCollection services)
    {
        public void ConfigurePersistence(IConfigurationManager configurationManager,
                                         bool isDev)
        {
            ConfigureDatabase(services, configurationManager, isDev);

            services.TryAddSingleton<IClock>(SystemClock.Instance);
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITransactionProvider, UnitOfWork>();
        }
    }
}
