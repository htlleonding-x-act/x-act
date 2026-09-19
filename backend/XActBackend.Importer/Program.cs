using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XActBackend.Persistence.Util;

namespace XActBackend.Importer;

// no top level statements: the integration tests reference this project and the api, and two generated
// entry points would clash
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
