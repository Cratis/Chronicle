// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Records reactor deliveries independently of the kernel.
/// </summary>
/// <param name="client">The caller-owned ledger client.</param>
/// <param name="runId">The isolated run identifier.</param>
internal sealed class SideLedger(IMongoClient client, string runId)
{
    readonly IMongoCollection<BsonDocument> _deliveries = client.GetDatabase($"chaos-{runId}")
        .GetCollection<BsonDocument>("deliveries").WithWriteConcern(WriteConcern.WMajority.With(journal: true));

    /// <summary>
    /// Durably records a delivery without deduplication.
    /// </summary>
    /// <param name="ledgerId">The intention identifier.</param>
    /// <param name="source">The event source.</param>
    /// <param name="sequence">The event sequence number.</param>
    /// <returns>The insert task.</returns>
    public Task Record(string ledgerId, string source, ulong sequence) => _deliveries.InsertOneAsync(new BsonDocument
    {
        { "_id", Guid.NewGuid().ToString("N") }, { "ledgerId", ledgerId }, { "source", source }, { "sequence", sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) }
    });

    /// <summary>
    /// Reads all delivered intention identifiers.
    /// </summary>
    /// <returns>The delivery snapshot.</returns>
    public async Task<IReadOnlyList<string>> Read() => (await _deliveries.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync()).Select(document => document["ledgerId"].AsString).ToArray();
}
