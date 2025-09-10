using MongoDB.Driver;
using System.Reflection;

namespace Shlongo
{
    public class MongrationContext(IMongoClient mongoClient, string databaseName, Assembly mongrationAssembly) : IMongrationContext
    {
        public IMongoClient MongoClient { get; } = mongoClient;
        public IMongoDatabase Database { get; } = mongoClient.GetDatabase(databaseName);
        public Mongration[] Mongrations { get; } = [.. mongrationAssembly
            .GetTypes()
            .Where(x => x.BaseType == typeof(Mongration))
            .Select(x => (Mongration)Activator.CreateInstance(x)!)
            .OrderBy(x => x.Id)];
    }
}
