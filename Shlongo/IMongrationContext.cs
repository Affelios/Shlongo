using MongoDB.Driver;

namespace Shlongo
{
    public interface IMongrationContext
    {
        public IMongoClient MongoClient { get; }
        public IMongoDatabase Database { get; }
    }
}
