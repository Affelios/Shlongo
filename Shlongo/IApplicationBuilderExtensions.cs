using Microsoft.AspNetCore.Builder;

namespace Shlongo
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseShlongo(this IApplicationBuilder app)
        {
            return app;
        }
    }
}
