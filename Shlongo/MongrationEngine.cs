namespace Shlongo
{
    class MongrationEngine
    {
        public async Task MongrateAsync(MongrationContext mongrationContext)
        {
            foreach (var mongration in mongrationContext.Mongrations)
            {
                await mongration.UpAsync(mongrationContext);
            }
        }
    }
}
