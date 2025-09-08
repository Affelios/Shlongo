using MongoDB.Driver;
using Shlongo;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddShlongo(this IServiceCollection services, MongoClient mongoClient, string databaseName, Assembly mongrationAssembly)
        {
            services.AddSingleton(new MongrationContext(mongoClient, databaseName, mongrationAssembly));
            services.AddHostedService<MongrationService>();
        }
    }
}
