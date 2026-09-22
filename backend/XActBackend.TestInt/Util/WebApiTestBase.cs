using System.Text.Json;
using XActBackend.Persistence.Util;
using XActBackend.Shared;

namespace XActBackend.TestInt.Util;

public abstract class WebApiTestBase(WebApiTestFixture webApiFixture) : IClassFixture<WebApiTestFixture>, IAsyncLifetime
{
    private static readonly Lazy<JsonSerializerOptions> jsonOptions = new(() =>
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        JsonConfig.ConfigureJsonSerialization(options, false);

        return options;
    });

    protected static JsonSerializerOptions JsonOptions => jsonOptions.Value;

    protected HttpClient ApiClient => webApiFixture.Client;
    protected IClock TestClock => webApiFixture.Clock;
    protected CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;
    
    public async ValueTask InitializeAsync()
    {
        await webApiFixture.RestoreDatabaseAsync(ImportSeedDataAsync);
    }

    public ValueTask DisposeAsync()
    {
        // nothing to clean up here, the database gets reset in InitializeAsync so even an interrupted run
        // starts clean
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask ImportSeedDataAsync(DatabaseContext context)
    {
        // seed data for every test goes here, derived classes override this to add their own
        return ValueTask.CompletedTask;
    }

    protected async ValueTask ModifyDatabaseContentAsync(Func<DatabaseContext, ValueTask> modifier)
    {
        await webApiFixture.ModifyDatabaseContentAsync(modifier);
    }
}
