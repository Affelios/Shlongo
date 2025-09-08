namespace Shlongo
{
    public abstract class Mongration
    {
        public abstract Task UpAsync(IMongrationContext context);
        public virtual Task DownAsync(IMongrationContext context)
        {
            return Task.CompletedTask;
        }
    }
}
