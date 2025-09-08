namespace Shlongo
{
    class MongrationEngine
    {
        public Task MongrateAsync(IMongrationContext mongrationContext)
        {
            var mongrations = mongrationContext.Mongrations;
            return Task.CompletedTask;
        }
    }
}
