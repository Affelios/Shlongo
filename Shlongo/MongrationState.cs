using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Shlongo
{
    public enum MongrationStatus
    {
        Running = 0,
        Success = 1,
        Failure = 2
    }

    public class MongrationState
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string? MongrationNamespace { get; set; }

        public int MongrationId { get; set; }

        public string MongrationName { get; set; } = string.Empty;

        /// <summary>
        /// When the mongration started (insert / Running).
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// When the mongration finished (Success or Failure). Null while Running.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Compatibility timestamp: equals <see cref="StartedAt"/> while Running,
        /// and equals <see cref="CompletedAt"/> after Success or Failure.
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid BatchId { get; set; }

        public MongrationStatus Status { get; set; } = MongrationStatus.Running;

        public string? Exception { get; set; }
    }
}
