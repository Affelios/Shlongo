using MongoDB.Driver;
using System.Reflection;

namespace Shlongo
{
    public class ShlongoConfiguration
    {
        public MongoClientSettings MongoClientSettings { get; set; } = new();
        public Assembly MongrationAssembly { get; set; } = Assembly.GetEntryAssembly()!;
        public string MongrationStateCollectionName { get; set; } = "MongrationState";
    }
}
