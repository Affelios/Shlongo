namespace Shlongo
{
    public abstract class Mongration
    {
        public int Id => int.Parse(GetType().Name.Split('_')[1]);

        public string Name => GetType().Name;

        public abstract Task UpAsync(IMongrationContext context);
    }
}
