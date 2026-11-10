// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Persists append intentions and outcomes independently of the kernel.
/// </summary>
/// <param name="client">The caller-owned ledger client.</param>
/// <param name="runId">The isolated run identifier.</param>
internal sealed class Ledger(IMongoClient client, string runId)
{
    readonly IMongoCollection<BsonDocument> _entries = client.GetDatabase($"chaos-{runId}")
        .GetCollection<BsonDocument>("intentions").WithWriteConcern(WriteConcern.WMajority.With(journal: true));

    /// <summary>
    /// Durably records an intention before appending.
    /// </summary>
    /// <param name="id">The idempotency identifier.</param>
    /// <param name="source">The event source.</param>
    /// <param name="event">The intended contribution.</param>
    /// <returns>The insert task.</returns>
    public Task Intend(string id, string source, ChaosEvent @event) => _entries.InsertOneAsync(new BsonDocument
    {
        { "_id", id }, { "source", source }, { "payload", @event.Payload },
        { "timestamp", @event.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture) }, { "status", "intended" }
    });

    /// <summary>
    /// Finds an intention without an acknowledgement for retry.
    /// </summary>
    /// <returns>The pending intention and its event, if any.</returns>
    public async Task<(LedgerEntry Entry, ChaosEvent Event)?> Pending()
    {
        var document = await _entries.Find(Builders<BsonDocument>.Filter.Ne("status", "acked")).FirstOrDefaultAsync();
        if (document is null) return null;
        var entry = From(document);
        var timestamp = DateTimeOffset.Parse(document["timestamp"].AsString, System.Globalization.CultureInfo.InvariantCulture);
        return (entry, new(entry.Id, timestamp, entry.Payload));
    }

    /// <summary>
    /// Records the observed append outcome.
    /// </summary>
    /// <param name="id">The intention identifier.</param>
    /// <param name="status">The outcome status.</param>
    /// <returns>The update task.</returns>
    public Task Outcome(string id, string status) => _entries.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), Builders<BsonDocument>.Update.Set("status", status));

    /// <summary>
    /// Reads all recorded intentions.
    /// </summary>
    /// <returns>The intention snapshot.</returns>
    public async Task<IReadOnlyList<LedgerEntry>> Read() => (await _entries.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync()).Select(From).ToArray();

    static LedgerEntry From(BsonDocument document) => new(document["_id"].AsString, document["source"].AsString, document["payload"].ToInt64(), document["status"].AsString);
}
