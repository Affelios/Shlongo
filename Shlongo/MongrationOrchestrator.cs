using Microsoft.Extensions.Logging;

namespace Shlongo
{
    public class MongrationOrchestrator(ILogger logger, MongrationContext context)
    {
        public async Task MongrateAsync()
        {
            var engines = context.Configuration.ModuleNamespaces is not null
                ? context.Configuration.ModuleNamespaces.Select(x => new MongrationEngine(logger, context.ToNamespace(x!))).ToArray()
                : [new MongrationEngine(logger, context)];

            foreach (var engine in engines)
            {
                await engine.MongrateAsync();
            }
        }
    }
}
