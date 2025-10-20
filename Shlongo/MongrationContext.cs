using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Shlongo
{
    public class MongrationContext(IServiceProvider provider, MongrationModel model) : IMongrationContext
    {
        public ShlongoConfiguration Configuration { get; } = model.Configuration;
        public IMongoClient MongoClient { get; } = model.MongoClient;
        public IMongoDatabase Database { get; private set; } = model.MongoClient.GetDatabase(model.Configuration.MongoDatabaseName);
        public Mongration[] Mongrations { get; } = [.. model.Configuration.MongrationAssembly
            .GetTypes()
            .Where(x => x.BaseType == typeof(Mongration))
            .Where(x => model.Configuration.Namespace is null || x.Namespace!.StartsWith(model.Configuration.Namespace))
            .Select(x => (Mongration)ActivatorUtilities.CreateInstance(provider, x)!)
            .OrderBy(x => x.Id)];
        public IClientSessionHandle Session { get; private set; } = null!;
        public void SetSession(IClientSessionHandle session)
        {
            Session = session;
        }

        public MongrationContext ToModule(ShlongoModule module)
        {
            return new(provider, model.ToModule(module))
            {
                Database = module.Database is null ? Database : MongoClient.GetDatabase(module.Database)
            };
        }
    }
}
