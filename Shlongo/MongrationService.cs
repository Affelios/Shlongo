using Microsoft.Extensions.Hosting;
using Shlongo;

namespace Microsoft.Extensions.DependencyInjection
{
    public class MongrationService(IMongrationContext mongrationContext) : IHostedService
    {
        private readonly MongrationEngine engine = new();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await engine.MongrateAsync(mongrationContext);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}