namespace Shlongo.Examples.Api.Mongrations
{
    public class User
    {
        public string Name { get; set; }
    }

    public class _0002_CreateIndexes : Mongration
    {
        public override async Task UpAsync(IMongrationContext context)
        {
            await context.Database.GetCollection<User>("Users").InsertManyAsync(new[]
            {
                new User { Name = "Alice" }
            });

            throw new Exception("Simulated failure during index creation.");
            // return Task.CompletedTask; // Unreachable code, can be removed
        }
    }
}
