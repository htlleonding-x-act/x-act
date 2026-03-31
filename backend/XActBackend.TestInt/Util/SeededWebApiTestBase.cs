using XActBackend.Importer;
using XActBackend.Persistence.Util;

namespace XActBackend.TestInt.Util;

public abstract class SeededWebApiTestBase(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    protected override ValueTask ImportSeedDataAsync(DatabaseContext context)
    {
        return new ValueTask(Seeder.InsertInitialSeedData(context));
    }
}
