using MongoDB.Driver;

namespace Shlongo
{
    public class MongrationContext(IMongoClient mongoClient, ShlongoConfiguration configuration) : IMongrationContext
    {
        public ShlongoConfiguration Configuration { get; } = configuration;
        public IMongoClient MongoClient { get; } = mongoClient;
        public IMongoDatabase Database { get; private set; } = mongoClient.GetDatabase(configuration.MongoDatabaseName);
        public Mongration[] Mongrations { get; } = [.. configuration.MongrationAssembly
            .GetTypes()
            .Where(x => x.BaseType == typeof(Mongration))
            .Where(x => configuration.Namespace is null || x.Namespace!.StartsWith(configuration.Namespace))
            .Select(x => (Mongration)Activator.CreateInstance(x)!)
            .OrderBy(x => x.Id)];
        public IClientSessionHandle Session { get; private set; } = null!;
        public void SetSession(IClientSessionHandle session)
        {
            Session = session;
        }

        public MongrationContext ToModule(ShlongoModule module)
        {
            return new(MongoClient, configuration.ToModule(module))
            {
                Database = module.Database is null ? Database : MongoClient.GetDatabase(module.Database)
            };
        }
    }
}
