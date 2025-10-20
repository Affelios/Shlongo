namespace Shlongo.Examples.Api.Mongrations
{
    public class _0001_CreateCollections(TestDependency testDependency, ILogger<_0001_CreateCollections> logger) : Mongration
    {
        public override async Task UpAsync(IMongrationContext context)
        {
            await context.Database.CreateCollectionAsync("Users");

            logger.LogInformation($"testDependency.Exists = {testDependency.Exists}");
        }
    }
}
