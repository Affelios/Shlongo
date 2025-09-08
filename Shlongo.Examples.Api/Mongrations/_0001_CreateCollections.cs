namespace Shlongo.Examples.Api.Mongrations
{
    public class _0001_CreateCollections : Mongration
    {
        public override async Task UpAsync(IMongrationContext context)
        {
            await context.Database.CreateCollectionAsync("Users");
        }
    }
}
