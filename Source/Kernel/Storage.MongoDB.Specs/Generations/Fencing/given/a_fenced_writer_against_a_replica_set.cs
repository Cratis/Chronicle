// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.given;

public class a_fenced_writer_against_a_replica_set(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected const string MaterializationKey = "store/namespace/model";
    protected const ulong Generation = 1;
    protected const int DocumentsPerBatch = 3;
    protected static readonly FenceRetryPolicy _policy = new(100000, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(90));

    protected IMongoClient _client;
    protected IMongoDatabase _database;
    protected FenceGates _gates;
    protected FenceToken _token;
    protected PausingProbe _probe;
    protected MongoFenceStore _store;
    protected FencedBatchExecutor _executor;
    protected List<string> _committedBeforeRevocation;
    protected List<string> _revoked;
    protected IReadOnlyList<string> _violations;
    string _databaseName;

    protected string ConnectionString => fixture.ConnectionString;

    /// <summary>
    /// Gets a value indicating whether the stale writers bypass the fence; set once to prove the oracle catches it (non-vacuity).
    /// </summary>
    protected static bool FenceDisabled => Environment.GetEnvironmentVariable("CHRONICLE_FENCE_SPECS_UNFENCED") == "1";

    protected IMongoCollection<BsonDocument> History => _database.GetCollection<BsonDocument>("history");

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_databaseName);
        await _database.CreateCollectionAsync("history");
        _gates = new(_database, _policy);
        _token = await _gates.Claim(MaterializationKey, Generation);
        _probe = new();
        _store = new(_database, _policy);
        _executor = new(_store, _policy, _probe);
        _committedBeforeRevocation = [];
        _revoked = [];
    }

    void Destroy()
    {
        _client.DropDatabase(_databaseName);
        _client.Dispose();
    }

    protected Task<int> WriteBatch(string label, FenceToken? token = null, IFencedBatchExecutor? executor = null)
    {
        var writeToken = token ?? _token;
        return (executor ?? _executor).Execute(writeToken, async session =>
        {
            for (var index = 0; index < DocumentsPerBatch; index++)
            {
                await History.InsertOneAsync(session, FenceHistoryOracle.Document(label, index, writeToken.Epoch));
                if (index == 0)
                {
                    await _probe.MidExecution();
                }
            }

            return DocumentsPerBatch;
        });
    }

    protected async Task WriteUnfenced(string label, long epoch)
    {
        for (var index = 0; index < DocumentsPerBatch; index++)
        {
            await History.InsertOneAsync(FenceHistoryOracle.Document(label, index, epoch));
        }
    }

    protected async Task CommitBeforeRevocation(string label)
    {
        await WriteBatch(label);
        _committedBeforeRevocation.Add(label);
    }

    protected Task<long> Revoke() => _gates.Revoke(MaterializationKey, Generation);

    protected async Task<Exception?> StaleBatch(string label)
    {
        _revoked.Add(label);
        if (FenceDisabled)
        {
            await WriteUnfenced(label, _token.Epoch);
            return null;
        }

        return await Catch.Exception(() => WriteBatch(label));
    }

    protected async Task Judge() =>
        _violations = await FenceHistoryOracle.Check(History, _token.Epoch, DocumentsPerBatch, _committedBeforeRevocation, _revoked);

    protected async Task SetTransactionLifetime(int seconds)
    {
        // A client of its own, so this also works from a Destroy that runs after the shared client is disposed.
        using var admin = new MongoClient(ConnectionString);
        await admin.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument { { "setParameter", 1 }, { "transactionLifetimeLimitSeconds", seconds } });
    }
}
