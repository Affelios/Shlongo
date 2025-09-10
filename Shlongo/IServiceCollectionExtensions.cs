using MongoDB.Driver;
using Shlongo;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddShlongo(this IServiceCollection services, Action<ShlongoConfiguration> c)
        {
            var configuration = new ShlongoConfiguration();
            
            c.Invoke(configuration);

            var mongoClient = new MongoClient(configuration.MongoClientSettings);
            services.AddSingleton(new MongrationContext(mongoClient, configuration));
            services.AddHostedService<MongrationService>();
        }
    }
}
