using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shlongo;

namespace Microsoft.Extensions.DependencyInjection
{
    public class MongrationService(MongrationContext mongrationContext, ILogger<MongrationService> logger) : IHostedService
    {
        private readonly MongrationOrchestrator orchestrator = new(logger, mongrationContext);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await orchestrator.MongrateAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}