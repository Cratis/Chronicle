// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// The server-side counters of one replica-set member that the fence cost is measured with.
/// </summary>
/// <param name="CpuMicroseconds">The mongod process's user plus system CPU time, in microseconds.</param>
/// <param name="Inserts">The insert opcounter.</param>
/// <param name="Updates">The update opcounter.</param>
/// <param name="Deletes">The delete opcounter.</param>
/// <param name="Commands">The command opcounter.</param>
/// <param name="TransactionsStarted">Transactions started.</param>
/// <param name="TransactionsCommitted">Transactions committed.</param>
/// <param name="TransactionsAborted">Transactions aborted.</param>
/// <param name="OplogBytes">The oplog's data size in bytes.</param>
internal sealed record FenceCostServerStats(
    long CpuMicroseconds,
    long Inserts,
    long Updates,
    long Deletes,
    long Commands,
    long TransactionsStarted,
    long TransactionsCommitted,
    long TransactionsAborted,
    long OplogBytes)
{
    /// <summary>
    /// Reads the counters of one member.
    /// </summary>
    /// <param name="client">A client connected directly to the member.</param>
    /// <returns>The counters.</returns>
    internal static async Task<FenceCostServerStats> Read(IMongoClient client)
    {
        var status = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("serverStatus", 1));
        var oplog = await client.GetDatabase("local").RunCommandAsync<BsonDocument>(new BsonDocument("collStats", "oplog.rs"));
        var extra = status["extra_info"].AsBsonDocument;
        var opcounters = status["opcounters"].AsBsonDocument;
        var transactions = status["transactions"].AsBsonDocument;
        return new(
            extra["user_time_us"].ToInt64() + extra["system_time_us"].ToInt64(),
            opcounters["insert"].ToInt64(),
            opcounters["update"].ToInt64(),
            opcounters["delete"].ToInt64(),
            opcounters["command"].ToInt64(),
            transactions["totalStarted"].ToInt64(),
            transactions["totalCommitted"].ToInt64(),
            transactions["totalAborted"].ToInt64(),
            oplog["size"].ToInt64());
    }

    /// <summary>
    /// Subtracts an earlier reading.
    /// </summary>
    /// <param name="earlier">The earlier reading.</param>
    /// <returns>The difference.</returns>
    internal FenceCostServerStats Since(FenceCostServerStats earlier) => new(
        CpuMicroseconds - earlier.CpuMicroseconds,
        Inserts - earlier.Inserts,
        Updates - earlier.Updates,
        Deletes - earlier.Deletes,
        Commands - earlier.Commands,
        TransactionsStarted - earlier.TransactionsStarted,
        TransactionsCommitted - earlier.TransactionsCommitted,
        TransactionsAborted - earlier.TransactionsAborted,
        OplogBytes - earlier.OplogBytes);
}
