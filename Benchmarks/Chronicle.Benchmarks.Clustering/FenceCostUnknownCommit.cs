// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures how long resolving an unknown commit outcome takes, for a batch that committed, one that did not, and one whose
/// transaction was left open by a writer that went away.
/// </summary>
internal static class FenceCostUnknownCommit
{
    /// <summary>
    /// Runs the measurements.
    /// </summary>
    /// <param name="replicaSet">The replica set.</param>
    /// <param name="repetitions">How many times to resolve a committed and a not-committed batch.</param>
    /// <param name="transactionLifetimeSeconds">The transaction lifetime limit to set while measuring the left-open case.</param>
    /// <returns>An object holding the measurements, ready to serialize.</returns>
    internal static async Task<object> Measure(FenceCostReplicaSet replicaSet, int repetitions, int transactionLifetimeSeconds)
    {
        using var client = new MongoClient(replicaSet.PrimaryConnectionString);
        var database = client.GetDatabase("fence-cost-unknown");
        await FenceStorage.EnsureCollections(database);
        var store = new MongoFenceStore(database, FenceRetryPolicy.Default);
        var token = await new FenceGates(database, FenceRetryPolicy.Default).Claim("unknown", 1);

        var committed = new List<double>();
        var notCommitted = new List<double>();
        for (var repetition = 0; repetition < repetitions; repetition++)
        {
            committed.Add(await Time(store, token, await Leave(store, token, commit: true)));
            notCommitted.Add(await Time(store, token, await Leave(store, token, commit: false)));
        }

        var admin = client.GetDatabase("admin");
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "setParameter", 1 }, { "transactionLifetimeLimitSeconds", transactionLifetimeSeconds } });
        double leftOpen;
        try
        {
            var open = await store.Begin(CancellationToken.None);
            var batch = new FenceBatchId(Guid.NewGuid());
            await open.Admit(token, batch, CancellationToken.None);
            leftOpen = await Time(store, token, batch);
            await open.Abort();
            open.Dispose();
        }
        finally
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "setParameter", 1 }, { "transactionLifetimeLimitSeconds", 60 } });
        }

        await client.DropDatabaseAsync("fence-cost-unknown");
        return new
        {
            Scenario = "unknown-commit-resolution",
            replicaSet.Members,
            CommittedMedianMs = Median(committed),
            CommittedMaxMs = committed.Max(),
            NotCommittedMedianMs = Median(notCommitted),
            NotCommittedMaxMs = notCommitted.Max(),
            TransactionLifetimeLimitSeconds = transactionLifetimeSeconds,
            LeftOpenMs = leftOpen
        };
    }

    static async Task<FenceBatchId> Leave(MongoFenceStore store, FenceToken token, bool commit)
    {
        using var transaction = await store.Begin(CancellationToken.None);
        var batch = new FenceBatchId(Guid.NewGuid());
        await transaction.Admit(token, batch, CancellationToken.None);
        await transaction.WriteReceipt(token, batch, CancellationToken.None);
        if (commit)
        {
            await transaction.Commit(CancellationToken.None);
        }
        else
        {
            await transaction.Abort();
        }

        return batch;
    }

    static async Task<double> Time(MongoFenceStore store, FenceToken token, FenceBatchId batch)
    {
        var started = Stopwatch.GetTimestamp();
        await store.Resolve(token, batch, CancellationToken.None);
        return Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    static double Median(List<double> values) => values.Order().ElementAt(values.Count / 2);
}
