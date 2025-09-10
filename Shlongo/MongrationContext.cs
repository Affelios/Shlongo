using MongoDB.Driver;

namespace Shlongo
{
    public class MongrationContext(IMongoClient mongoClient, ShlongoConfiguration configuration) : IMongrationContext
    {
        public ShlongoConfiguration Configuration { get; } = configuration;
        public IMongoClient MongoClient { get; } = mongoClient;
        public IMongoDatabase Database { get; } = mongoClient.GetDatabase(configuration.MongoDatabaseName);
        public Mongration[] Mongrations { get; } = [.. configuration.MongrationAssembly
            .GetTypes()
            .Where(x => x.BaseType == typeof(Mongration))
            .Select(x => (Mongration)Activator.CreateInstance(x)!)
            .OrderBy(x => x.Id)];
        public IClientSessionHandle Session { get; private set; } = null!;
        public void SetSession(IClientSessionHandle session)
        {
            Session = session;
        }
    }
}
