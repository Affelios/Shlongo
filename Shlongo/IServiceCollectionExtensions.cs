using MongoDB.Driver;
using Shlongo;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddShlongo(this IServiceCollection services, Action<ShlongoConfiguration> c)
        {
            var configuration = (ShlongoConfiguration)c.Target!;

            var mongoClient = new MongoClient(configuration.MongoClientSettings);
            services.AddSingleton(new MongrationContext(
                mongoClient,
                configuration.MongrationStateCollectionName,
                configuration.MongrationAssembly));
            services.AddHostedService<MongrationService>();
        }
    }
}
