using MongoDB.Driver;
using System.Reflection;

namespace Shlongo
{
    public class ShlongoConfiguration
    {
        public MongoClientSettings MongoClientSettings { get; set; } = new();
        public Assembly MongrationAssembly { get; set; } = Assembly.GetEntryAssembly()!;
        public string? MongoDatabaseName { get; set; }
        public string MongrationStateCollectionName { get; set; } = "_mongrations";
    }
}
