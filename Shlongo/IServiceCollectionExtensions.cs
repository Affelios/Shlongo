namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddShlongo(this IServiceCollection services)
        {
            services.AddHostedService<MongrationService>();
        }
    }
}
