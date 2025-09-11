using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shlongo
{
	public class MongrationEngine(ILogger logger, MongrationContext context)
	{
		private IMongoCollection<MongrationState> GetMongrationCollection()
		{
			return context.Database.GetCollection<MongrationState>(context.Configuration.MongrationStateCollectionName);
		}

		public async Task MongrateAsync()
        {
            await CheckForBlockingStatesAsync("apply");

            var lastExecutedMongration = await GetLastExecutedMongrationAsync();
			var lastExecutedMongrationId = lastExecutedMongration is null ? 0 : lastExecutedMongration.MongrationId;

            var pendingMongrations = context.Mongrations
                .Where(m => m.Id > lastExecutedMongrationId)
                .ToArray();

            if (pendingMongrations.Length == 0)
            {
                logger.LogInformation("No pending mongrations found.");
                return;
            }

            var batchId = Guid.NewGuid();
            logger.LogInformation("Executing {MongrationCount} mongrations in batch {BatchId}",
                pendingMongrations.Length, batchId);

            var stopwatch = Stopwatch.StartNew();

			var activeMongration = default(Mongration);

            try
            {
                foreach (var mongration in pendingMongrations)
                {
					activeMongration = mongration;

                    var session = new MongoSession(context.MongoClient);

                    await session.StartSessionAsync();

                    session.StartTransaction();

                    try
					{
                        await RecordMongrationStartAsync(mongration, batchId);

                        context.SetSession(session.Session);

                        logger.LogInformation("Executing mongration: {MongrationName}", mongration.Name);

                        await mongration.UpAsync(context);

                        await UpdateMongrationSuccessAsync(mongration.Id);

                        await session.CommitTransactionAsync();

                        logger.LogInformation("Mongration {MongrationName} completed successfully in {ExecutionTimeMs}ms",
                            mongration.Name, stopwatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Mongration {MongrationName} failed after {ExecutionTimeMs}ms",
							mongration.Name, stopwatch.ElapsedMilliseconds);

                        await session.AbortTransactionAsync();

                        await UpdateMongrationFailureAsync(mongration.Id, ex);
                    }
                }
            }
			catch (Exception)
            {
                stopwatch.Stop();
				
                logger.LogCritical("Halting remaining mongrations due to failure in {MongrationName}.", activeMongration!.Name);

				throw;
			}
			
            logger.LogInformation("Batch {BatchId} completed", batchId);
        }

        private async Task CheckForBlockingStatesAsync(string actionName)
        {
            var blockingStates = await GetBlockingStatesAsync();

            if (blockingStates.Count != 0)
            {
                var failed = string.Join(',', blockingStates.Select(x => x.MongrationName));
                var error = $"Unable to {actionName} mongrations. Database may be in an inconsistent state, mongrations failed: {failed}.";
                logger.LogCritical(error);
                throw new Exception(error);
            }
        }

        private async Task<MongrationState> RecordMongrationStartAsync(Mongration mongration, Guid batchId)
        {
            var collection = GetMongrationCollection();

			var state = new MongrationState
			{
				MongrationId = mongration.Id,
				MongrationName = mongration.Name,
                MongrationNamespace = context.Configuration.Namespace,
				ExecutedAt = DateTime.UtcNow,
				BatchId = batchId
			};

            await collection.InsertOneAsync(state);

			return state;
        }

		private async Task<List<MongrationState>> GetBlockingStatesAsync()
        {
            var blockingFilter = Builders<MongrationState>.Filter.And(
                Builders<MongrationState>.Filter.Eq(x => x.MongrationNamespace, context.Configuration.Namespace),
                Builders<MongrationState>.Filter.In(x => x.Status, [MongrationStatus.Running, MongrationStatus.Failure])
            );
			var blockingStates = await GetMongrationCollection().Find(blockingFilter).ToListAsync();
            return blockingStates;
		}

		private async Task UpdateMongrationSuccessAsync(int mongrationId)
        {
            var filter = Builders<MongrationState>.Filter.And(
                Builders<MongrationState>.Filter.Eq(x => x.MongrationNamespace, context.Configuration.Namespace),
                Builders<MongrationState>.Filter.Eq(x => x.MongrationId, mongrationId)
            );
			var update = Builders<MongrationState>.Update
				.Set(x => x.Status, MongrationStatus.Success)
				.Set(x => x.ExecutedAt, DateTime.UtcNow);
			await GetMongrationCollection().UpdateOneAsync(filter, update);
		}

		private async Task UpdateMongrationFailureAsync(int mongrationId, Exception ex)
        {
            var filter = Builders<MongrationState>.Filter.And(
                Builders<MongrationState>.Filter.Eq(x => x.MongrationNamespace, context.Configuration.Namespace),
                Builders<MongrationState>.Filter.Eq(x => x.MongrationId, mongrationId)
            );
            var update = Builders<MongrationState>.Update
				.Set(x => x.Status, MongrationStatus.Failure)
				.Set(x => x.ExecutedAt, DateTime.UtcNow)
				.Set(x => x.Exception, ex.ToString());
			await GetMongrationCollection().UpdateOneAsync(filter, update);
		}

		private async Task<MongrationState?> GetLastExecutedMongrationAsync()
		{
            var filter = Builders<MongrationState>.Filter.And(
                Builders<MongrationState>.Filter.Eq(x => x.MongrationNamespace, context.Configuration.Namespace),
                Builders<MongrationState>.Filter.Eq(x => x.Status, MongrationStatus.Success)
            );
			var lastExecutedMongration = await GetMongrationCollection()
				.Find(filter)
				.Sort(Builders<MongrationState>.Sort.Ascending(x => x.MongrationId))
				.FirstOrDefaultAsync();
			return lastExecutedMongration;
		}
	}
}
