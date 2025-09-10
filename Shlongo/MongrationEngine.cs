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
			if (await HasBlockingStatesAsync())
			{
				logger.LogWarning("Mongrations will not run because a previous or current mongration is in Running or Failure state.");
				return;
			}
			
			var executedMongrations = await GetExecutedMongrationsAsync();
			var pendingMongrations = context.Mongrations
				.Where(m => !executedMongrations.Any(em => em.MongrationName == m.Name))
				.ToArray();

			if (pendingMongrations.Length == 0)
			{
				logger.LogInformation("No pending mongrations found.");
				return;
			}

			var batchId = await GetNextBatchIdAsync();
			logger.LogInformation("Executing {MongrationCount} mongrations in batch {BatchId}", 
				pendingMongrations.Length, batchId);

			int successCount = 0;
			int failureCount = 0;

			foreach (var mongration in pendingMongrations)
			{
				var stopwatch = System.Diagnostics.Stopwatch.StartNew();
				string runningRecordId = string.Empty;
				try
				{
					runningRecordId = await RecordMongrationStartAsync(mongration, batchId);

					logger.LogInformation("Executing mongration: {MongrationName}", mongration.Name);
					await mongration.UpAsync(context);
					stopwatch.Stop();
					
					await UpdateMongrationSuccessAsync(runningRecordId);
					logger.LogInformation("Mongration {MongrationName} completed successfully in {ExecutionTimeMs}ms", 
						mongration.Name, stopwatch.ElapsedMilliseconds);
					successCount++;
				}
				catch (Exception ex)
				{
					stopwatch.Stop();
					logger.LogError(ex, "Mongration {MongrationName} failed after {ExecutionTimeMs}ms", 
						mongration.Name, stopwatch.ElapsedMilliseconds);
					if (!string.IsNullOrEmpty(runningRecordId))
					{
						await UpdateMongrationFailureAsync(runningRecordId, ex);
					}
					failureCount++;
					logger.LogWarning("Halting remaining mongrations due to failure in {MongrationName}.", mongration.Name);
					break;
				}
			}

			logger.LogInformation("Batch {BatchId} completed. Success: {SuccessCount}, Failures: {FailureCount}", 
				batchId, successCount, failureCount);
		}

		public async Task RollbackLastBatchAsync(MongrationContext context)
		{
			if (await HasBlockingStatesAsync())
			{
				logger.LogWarning("Rollback will not run because a mongration is in Running or Failure state.");
				return;
			}

			var lastBatchMongrations = await GetLastBatchMongrationsAsync();
			
			if (lastBatchMongrations.Count == 0)
			{
				logger.LogInformation("No mongrations to rollback.");
				return;
			}

			var batchId = lastBatchMongrations.First().BatchId;
			logger.LogInformation("Rolling back batch {BatchId} ({MongrationCount} mongrations)", 
				batchId, lastBatchMongrations.Count);

			var mongrationsToRollback = lastBatchMongrations
				.OrderByDescending(m => m.Id)
				.ToArray();

			foreach (var mongrationState in mongrationsToRollback)
			{
				var mongration = context.Mongrations.FirstOrDefault(m => m.GetType().Name == mongrationState.MongrationName);
				if (mongration == null)
				{
					logger.LogWarning("Mongration class {MongrationName} not found, recording rollback as success for record cleanup", 
						mongrationState.MongrationName);
					await RecordRollbackSuccessAsync(mongrationState);
					continue;
				}

				string rollbackRecordId = string.Empty;
				try
				{
					rollbackRecordId = await RecordRollbackStartAsync(mongrationState);
					logger.LogInformation("Rolling back mongration: {MongrationName}", mongrationState.MongrationName);
					await mongration.DownAsync(context);
					await UpdateRollbackSuccessAsync(rollbackRecordId);
					logger.LogInformation("Mongration {MongrationName} rolled back successfully", 
						mongrationState.MongrationName);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to rollback mongration {MongrationName}", mongrationState.MongrationName);
					if (!string.IsNullOrEmpty(rollbackRecordId))
					{
						await UpdateRollbackFailureAsync(rollbackRecordId, ex);
					}
					throw;
				}
			}

			logger.LogInformation("Batch {BatchId} rolled back successfully. {MongrationCount} mongrations rolled back.", 
				batchId, mongrationsToRollback.Length);
		}

		public async Task GetMongrationStatusAsync(MongrationContext context)
		{
			var executedMongrations = await GetExecutedMongrationsAsync();
			var allMongrations = context.Mongrations.ToList();

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

		private async Task<int> GetNextBatchIdAsync()
		{
			var lastBatch = await GetMongrationCollection()
				.Find(Builders<MongrationState>.Filter.Empty)
				.Sort(Builders<MongrationState>.Sort.Descending(x => x.BatchId))
				.Limit(1)
				.FirstOrDefaultAsync();

			return lastBatch?.BatchId + 1 ?? 1;
		}

		private async Task<bool> HasBlockingStatesAsync()
		{
			var blockingFilter = Builders<MongrationState>.Filter.In(x => x.Status, new[] { MongrationStatus.Running, MongrationStatus.Failure });
			return await GetMongrationCollection().Find(blockingFilter).Limit(1).AnyAsync();
		}

		private async Task<string> RecordMongrationStartAsync(Mongration mongration, int batchId)
		{
			var collection = GetMongrationCollection();
			var filter = Builders<MongrationState>.Filter.Eq(x => x.MongrationId, mongration.Id);
			var update = Builders<MongrationState>.Update
				.Set(x => x.MongrationId, mongration.Id)
				.Set(x => x.MongrationName, mongration.Name)
				.Set(x => x.ExecutedAt, DateTime.UtcNow)
				.Set(x => x.BatchId, batchId)
				.Set(x => x.Status, MongrationStatus.Running)
				.Set(x => x.IsRollback, false)
				.Set(x => x.Exception, null);
			var options = new FindOneAndUpdateOptions<MongrationState> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
			var result = await collection.FindOneAndUpdateAsync(filter, update, options);
			return result.Id;
		}

		private async Task UpdateMongrationSuccessAsync(string id)
		{
			var filter = Builders<MongrationState>.Filter.Eq(x => x.Id, id);
			var update = Builders<MongrationState>.Update
				.Set(x => x.Status, MongrationStatus.Success)
				.Set(x => x.ExecutedAt, DateTime.UtcNow);
			await GetMongrationCollection().UpdateOneAsync(filter, update);
		}

		private async Task UpdateMongrationFailureAsync(string id, Exception ex)
		{
			var filter = Builders<MongrationState>.Filter.Eq(x => x.Id, id);
			var update = Builders<MongrationState>.Update
				.Set(x => x.Status, MongrationStatus.Failure)
				.Set(x => x.ExecutedAt, DateTime.UtcNow)
				.Set(x => x.Exception, ex.ToString());
			await GetMongrationCollection().UpdateOneAsync(filter, update);
		}

		private async Task<string> RecordRollbackStartAsync(MongrationState original)
		{
			var collection = GetMongrationCollection();
			var filter = Builders<MongrationState>.Filter.Eq(x => x.MongrationId, original.MongrationId);
			var update = Builders<MongrationState>.Update
				.Set(x => x.MongrationId, original.MongrationId)
				.Set(x => x.MongrationName, original.MongrationName)
				.Set(x => x.ExecutedAt, DateTime.UtcNow)
				.Set(x => x.BatchId, original.BatchId)
				.Set(x => x.Status, MongrationStatus.Running)
				.Set(x => x.IsRollback, true)
				.Set(x => x.Exception, null);
			var options = new FindOneAndUpdateOptions<MongrationState> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
			var result = await collection.FindOneAndUpdateAsync(filter, update, options);
			return result.Id;
		}

		private async Task UpdateRollbackSuccessAsync(string id)
		{
			var filter = Builders<MongrationState>.Filter.Eq(x => x.Id, id);
			var update = Builders<MongrationState>.Update
				.Set(x => x.Status, MongrationStatus.Success)
				.Set(x => x.ExecutedAt, DateTime.UtcNow);
			await GetMongrationCollection().UpdateOneAsync(filter, update);
		}

		private async Task UpdateRollbackFailureAsync(string id, Exception ex)
		{
			var filter = Builders<MongrationState>.Filter.Eq(x => x.Id, id);
			var update = Builders<MongrationState>.Update
				.Set(x => x.Status, MongrationStatus.Failure)
				.Set(x => x.ExecutedAt, DateTime.UtcNow)
				.Set(x => x.Exception, ex.ToString());
			await GetMongrationCollection().UpdateOneAsync(filter, update);
		}

		private async Task RecordRollbackSuccessAsync(MongrationState original)
		{
			var id = await RecordRollbackStartAsync(original);
			await UpdateRollbackSuccessAsync(id);
		}

		private async Task<List<MongrationState>> GetExecutedMongrationsAsync()
		{
			var filter = Builders<MongrationState>.Filter.And(
				Builders<MongrationState>.Filter.Eq(x => x.Status, MongrationStatus.Success),
				Builders<MongrationState>.Filter.Eq(x => x.IsRollback, false)
			);
			return await GetMongrationCollection()
				.Find(filter)
				.Sort(Builders<MongrationState>.Sort.Ascending(x => x.BatchId).Ascending(x => x.ExecutedAt))
				.ToListAsync();
		}

		private async Task<List<MongrationState>> GetLastBatchMongrationsAsync()
		{
			var lastBatchId = await GetNextBatchIdAsync() - 1;
			if (lastBatchId < 1) return new List<MongrationState>();

			var filter = Builders<MongrationState>.Filter.And(
				Builders<MongrationState>.Filter.Eq(x => x.BatchId, lastBatchId),
				Builders<MongrationState>.Filter.Eq(x => x.Status, MongrationStatus.Success),
				Builders<MongrationState>.Filter.Eq(x => x.IsRollback, false)
			);

			return await GetMongrationCollection()
				.Find(filter)
				.Sort(Builders<MongrationState>.Sort.Descending(x => x.ExecutedAt))
				.ToListAsync();
		}
	}
}
