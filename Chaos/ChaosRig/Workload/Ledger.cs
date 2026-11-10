// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Chaos;

internal sealed class Ledger
{
    readonly IMongoCollection<BsonDocument> _entries;

    public Ledger(string connection, string runId) => _entries = new MongoClient(connection).GetDatabase($"chaos-{runId}")
        .GetCollection<BsonDocument>("intentions").WithWriteConcern(WriteConcern.WMajority.With(journal: true));

    public Task Intend(string id, string source, ChaosEvent @event) => _entries.InsertOneAsync(new BsonDocument
    {
        { "_id", id }, { "source", source }, { "payload", @event.Payload },
        { "timestamp", @event.Timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture) }, { "status", "intended" }
    });

    public async Task<(LedgerEntry Entry, ChaosEvent Event)?> Pending()
    {
        var document = await _entries.Find(Builders<BsonDocument>.Filter.Ne("status", "acked")).FirstOrDefaultAsync();
        if (document is null) return null;
        var entry = From(document);
        var timestamp = DateTimeOffset.Parse(document["timestamp"].AsString, System.Globalization.CultureInfo.InvariantCulture);
        return (entry, new(entry.Id, timestamp, entry.Payload));
    }

    public Task Outcome(string id, string status) => _entries.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id), Builders<BsonDocument>.Update.Set("status", status));

    public async Task<IReadOnlyList<LedgerEntry>> Read() => (await _entries.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync()).Select(From).ToArray();

    static LedgerEntry From(BsonDocument document) => new(document["_id"].AsString, document["source"].AsString, document["payload"].ToInt64(), document["status"].AsString);
}
