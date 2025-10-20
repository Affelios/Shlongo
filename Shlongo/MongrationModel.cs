using MongoDB.Driver;

namespace Shlongo
{
    public class MongrationModel(IMongoClient mongoClient, ShlongoConfiguration configuration)
    {
        public IMongoClient MongoClient { get; } = mongoClient;
        public ShlongoConfiguration Configuration { get; } = configuration;

        public MongrationModel ToModule(ShlongoModule module)
        {
            return new MongrationModel(MongoClient, Configuration.ToModule(module));
        }
    }
}
