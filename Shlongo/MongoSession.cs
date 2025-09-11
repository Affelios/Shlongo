using MongoDB.Driver;

namespace Shlongo
{
    public class MongoSession(IMongoClient mongoClient)
    {
        public IClientSessionHandle Session { get; private set; } = null!;

        public async Task StartSessionAsync()
        {
            Session = await mongoClient.StartSessionAsync();
        }

        public void StartTransaction()
        {
#if DEBUG
#else
            Session.StartTransaction();
#endif
        }

        public async Task AbortTransactionAsync()
        {
#if DEBUG
#else
            await Session.AbortTransactionAsync();
#endif
        }

        public async Task CommitTransactionAsync()
        {
#if DEBUG
#else
            await Session.CommitTransactionAsync();
#endif
        }
    }
}
