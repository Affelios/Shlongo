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

        public int MongrationId { get; set; }

        public string MongrationName { get; set; } = string.Empty;

        public DateTime ExecutedAt { get; set; }

        public Guid BatchId { get; set; }

        public MongrationStatus Status { get; set; } = MongrationStatus.Running;

        public string? Exception { get; set; }

        // Indicates this state represents a rollback attempt rather than a forward migration
        public bool IsRollback { get; set; }
    }
}
