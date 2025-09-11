using Microsoft.Extensions.Logging;

namespace Shlongo
{
    public class MongrationOrchestrator(ILogger logger, MongrationContext context)
    {
        public async Task MongrateAsync()
        {
            var engines = context.Configuration.Modules is not null
                ? context.Configuration.Modules.Select(x => new MongrationEngine(logger, context.ToModule(x!))).ToArray()
                : [new MongrationEngine(logger, context)];

            foreach (var engine in engines)
            {
                await engine.MongrateAsync();
            }
        }
    }
}
