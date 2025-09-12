using MongoDB.Driver;

namespace Shlongo
{
    public class MongoSession(IMongoClient mongoClient, bool disableTransactions)
    {
        public bool DisableTransactions { get; } = disableTransactions;
        public IClientSessionHandle Session { get; private set; } = null!;

        public async Task StartSessionAsync()
        {
            Session = await mongoClient.StartSessionAsync();
        }

        public void StartTransaction()
        {
            if (DisableTransactions)
            {
                return;
            }

            Session.StartTransaction();
        }

        public async Task AbortTransactionAsync()
        {
            if (DisableTransactions)
            {
                return;
            }

            await Session.AbortTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (DisableTransactions)
            {
                return;
            }

            await Session.CommitTransactionAsync();
        }
    }
}
