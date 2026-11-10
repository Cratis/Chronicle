// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Chaos;

internal sealed class SideLedger
{
    readonly IMongoCollection<BsonDocument> _deliveries;

    public SideLedger(string connection, string runId) => _deliveries = new MongoClient(connection).GetDatabase($"chaos-{runId}")
        .GetCollection<BsonDocument>("deliveries").WithWriteConcern(WriteConcern.WMajority.With(journal: true));

    public Task Record(string ledgerId, string source, ulong sequence) => _deliveries.InsertOneAsync(new BsonDocument
    {
        { "_id", Guid.NewGuid().ToString("N") }, { "ledgerId", ledgerId }, { "source", source }, { "sequence", sequence.ToString(System.Globalization.CultureInfo.InvariantCulture) }
    });

    public async Task<IReadOnlyList<string>> Read() => (await _deliveries.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync()).Select(document => document["ledgerId"].AsString).ToArray();
}
