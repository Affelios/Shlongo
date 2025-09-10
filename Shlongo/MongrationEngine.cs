using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace Shlongo
{
    public class MongrationEngine(ILogger logger, MongrationContext context)
    {
        private IMongoCollection<MongrationState> GetMongrationCollection()
        {
            return context.Database.GetCollection<MongrationState>("_mongrations");
        }

        public async Task MongrateAsync()
        {
            var mongrationsCollection = GetMongrationCollection();
            
            var executedMongrations = await GetExecutedMongrationsAsync();
            var executedMongrationNames = executedMongrations.Select(m => m.MongrationName).ToHashSet();
            
            var pendingMongrations = context.Mongrations
                .Where(m => !executedMongrationNames.Contains(m.Name))
                .ToArray();

            if (pendingMongrations.Length == 0)
            {
                logger.LogInformation("No pending mongrations found.");
                return;
            }

            var batchNumber = await GetNextBatchNumberAsync();
            logger.LogInformation("Executing {MongrationCount} mongrations in batch {BatchNumber}", 
                pendingMongrations.Length, batchNumber);

            foreach (var mongration in pendingMongrations)
            {
                var mongrationName = mongration.GetType().Name;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                try
                {
                    logger.LogInformation("Executing mongration: {MongrationName}", mongrationName);
                    await mongration.UpAsync(context);
                    stopwatch.Stop();
                    
                    await RecordMongrationExecutionAsync(mongrationName, batchNumber, stopwatch.ElapsedMilliseconds);
                    logger.LogInformation("Mongration {MongrationName} completed successfully in {ExecutionTimeMs}ms", 
                        mongrationName, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    logger.LogError(ex, "Mongration {MongrationName} failed after {ExecutionTimeMs}ms", 
                        mongrationName, stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }

            logger.LogInformation("Batch {BatchNumber} completed successfully. {MongrationCount} mongrations executed.", 
                batchNumber, pendingMongrations.Length);
        }

        public async Task RollbackLastBatchAsync(MongrationContext context)
        {
            var mongrationsCollection = GetMongrationCollection();

            var lastBatchMongrations = await GetLastBatchMongrationsAsync();
            
            if (lastBatchMongrations.Count == 0)
            {
                logger.LogInformation("No mongrations to rollback.");
                return;
            }

            var batchNumber = lastBatchMongrations.First().BatchNumber;
            logger.LogInformation("Rolling back batch {BatchNumber} ({MongrationCount} mongrations)", 
                batchNumber, lastBatchMongrations.Count);

            // Rollback mongrations in reverse order
            var mongrationsToRollback = lastBatchMongrations
                .OrderByDescending(m => m.ExecutedAt)
                .ToArray();

            foreach (var mongrationState in mongrationsToRollback)
            {
                var mongration = context.Mongrations.FirstOrDefault(m => m.GetType().Name == mongrationState.MongrationName);
                if (mongration == null)
                {
                    logger.LogWarning("Mongration class {MongrationName} not found, marking as rolled back", 
                        mongrationState.MongrationName);
                    await RecordMongrationRollbackAsync(mongrationState.MongrationName);
                    continue;
                }

                try
                {
                    logger.LogInformation("Rolling back mongration: {MongrationName}", mongrationState.MongrationName);
                    await mongration.DownAsync(context);
                    await RecordMongrationRollbackAsync(mongrationState.MongrationName);
                    logger.LogInformation("Mongration {MongrationName} rolled back successfully", 
                        mongrationState.MongrationName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to rollback mongration {MongrationName}", mongrationState.MongrationName);
                    throw;
                }
            }

            logger.LogInformation("Batch {BatchNumber} rolled back successfully. {MongrationCount} mongrations rolled back.", 
                batchNumber, mongrationsToRollback.Length);
        }

        public async Task GetMongrationStatusAsync(MongrationContext context)
        {
            var mongrationsCollection = GetMongrationCollection();

            var executedMongrations = await GetExecutedMongrationsAsync();
            var allMongrations = context.Mongrations.ToList();

            if (executedMongrations.Any())
            {
                logger.LogInformation("Executed mongrations:");
                foreach (var mongration in executedMongrations)
                {
                    var status = mongration.IsRolledBack ? "ROLLED BACK" : "EXECUTED";
                    var rollbackInfo = mongration.IsRolledBack ? $" (rolled back at {mongration.RolledBackAt:yyyy-MM-dd HH:mm:ss})" : "";
                    logger.LogInformation("  [{BatchNumber}] {MongrationName} - {Status} at {ExecutedAt:yyyy-MM-dd HH:mm:ss} ({ExecutionTimeMs}ms){RollbackInfo}", 
                        mongration.BatchNumber, mongration.MongrationName, status, mongration.ExecutedAt, mongration.ExecutionTimeMs, rollbackInfo);
                }
            }

            var pendingMongrations = allMongrations
                .Where(m => !executedMongrations.Any(em => em.MongrationName == m.GetType().Name))
                .ToList();

            if (pendingMongrations.Any())
            {
                logger.LogInformation("Pending mongrations:");
                foreach (var mongration in pendingMongrations)
                {
                    logger.LogInformation("  {MongrationName}", mongration.GetType().Name);
                }
            }
        }

        private async Task<int> GetNextBatchNumberAsync()
        {
            var mongrationsCollection = GetMongrationCollection();

            var lastBatch = await mongrationsCollection
                .Find(Builders<MongrationState>.Filter.Empty)
                .Sort(Builders<MongrationState>.Sort.Descending(x => x.BatchNumber))
                .Limit(1)
                .FirstOrDefaultAsync();

            return lastBatch?.BatchNumber + 1 ?? 1;
        }

        private async Task RecordMongrationExecutionAsync(string mongrationName, int batchNumber, long executionTimeMs)
        {
            var mongrationsCollection = GetMongrationCollection();

            var mongrationState = new MongrationState
            {
                MongrationName = mongrationName,
                ExecutedAt = DateTime.UtcNow,
                BatchNumber = batchNumber,
            };

            await mongrationsCollection.InsertOneAsync(mongrationState);
        }

        private async Task RecordMongrationRollbackAsync(string mongrationName)
        {
            var mongrationsCollection = GetMongrationCollection();

            var filter = Builders<MongrationState>.Filter.Eq(x => x.MongrationName, mongrationName);
            var update = Builders<MongrationState>.Update
                .Set(x => x.IsRolledBack, true)
                .Set(x => x.RolledBackAt, DateTime.UtcNow);

            await mongrationsCollection.UpdateOneAsync(filter, update);
        }

        private async Task<List<MongrationState>> GetExecutedMongrationsAsync()
        {
            var mongrationsCollection = GetMongrationCollection();

            var filter = Builders<MongrationState>.Filter.Eq(x => x.IsRolledBack, false);
            return await mongrationsCollection
                .Find(filter)
                .Sort(Builders<MongrationState>.Sort.Ascending(x => x.BatchNumber).Ascending(x => x.ExecutedAt))
                .ToListAsync();
        }

        private async Task<List<MongrationState>> GetLastBatchMongrationsAsync()
        {
            var mongrationsCollection = GetMongrationCollection();

            var lastBatchNumber = await GetNextBatchNumberAsync() - 1;
            if (lastBatchNumber < 1) return new List<MongrationState>();

            var filter = Builders<MongrationState>.Filter.And(
                Builders<MongrationState>.Filter.Eq(x => x.BatchNumber, lastBatchNumber),
                Builders<MongrationState>.Filter.Eq(x => x.IsRolledBack, false)
            );

            return await mongrationsCollection
                .Find(filter)
                .Sort(Builders<MongrationState>.Sort.Descending(x => x.ExecutedAt))
                .ToListAsync();
        }
    }
}
