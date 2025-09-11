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
        public List<ShlongoModule>? Modules { get; set; }
        public string? Namespace { get; set; }

        public ShlongoConfiguration ToModule(ShlongoModule module)
        {
            return new ShlongoConfiguration
            {
                MongoClientSettings = MongoClientSettings,
                MongrationAssembly = MongrationAssembly,
                MongoDatabaseName = module.Database ?? MongoDatabaseName,
                MongrationStateCollectionName = MongrationStateCollectionName,
                Modules = null,
                Namespace = module.Namespace
            };
        }
    }
}
