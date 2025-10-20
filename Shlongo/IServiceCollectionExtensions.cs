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
            var model = new MongrationModel(mongoClient, configuration);
            services.AddSingleton(model);
            services.AddSingleton<MongrationContext>();
            services.AddHostedService<MongrationService>();
        }
    }
}
