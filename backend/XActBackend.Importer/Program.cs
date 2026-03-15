using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XActBackend.Persistence.Util;

namespace XActBackend.Importer;

// avoid top level statements to avoid conflicts in int tests which reference two projects with entry points
internal static class Program
{
    private static async Task Main(string[] args)
    {
        var host = CreateHost(args);
        using var scope = host.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Program));

        bool inserted = await Seeder.InsertInitialSeedData(context);
        logger.LogInformation(inserted ? "Seed data inserted." : "Seed data already present.");
    }

    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.ConfigurePersistence(builder.Configuration, true);
        return builder.Build();
    }
}
