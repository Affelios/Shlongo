using Shlongo;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddShlongo(this IServiceCollection services, Assembly mongrationAssembly)
        {
            services.AddSingleton<IMongrationContext>(new MongrationContext(mongrationAssembly));
            services.AddHostedService<MongrationService>();
        }
    }
}
