// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences;

public partial class OccurrencesStorage
{
    /// <inheritdoc/>
    public async Task<bool> RetryQuarantined(OccurrenceId id, string principal, DateTimeOffset now)
    {
        var occurrence = await Get(id);
        if (occurrence is null || occurrence.State != OccurrenceState.Quarantined)
        {
            return false;
        }

        var audited = occurrence with { RetriedBy = principal, RetriedAt = now };
        var result = await _occurrences.UpdateOneAsync(
            Id(id.Value) & Builders<BsonDocument>.Filter.Eq("state", (int)OccurrenceState.Quarantined) & Builders<BsonDocument>.Filter.Eq("epoch", occurrence.Epoch),
            Builders<BsonDocument>.Update.Set("state", (int)OccurrenceState.Pending).Set("attempts", 0).Set("nextAttempt", BsonNull.Value).Set("data", JsonSerializer.Serialize(audited)));
        return result.ModifiedCount == 1;
    }

    /// <inheritdoc/>
    public async Task ExpireLeases(string worker, DateTimeOffset now) => await _occurrences.UpdateManyAsync(
        Builders<BsonDocument>.Filter.Eq("state", (int)OccurrenceState.Leased) & Builders<BsonDocument>.Filter.Eq("owner", worker) & Builders<BsonDocument>.Filter.Gt("expiry", now.UtcTicks),
        Builders<BsonDocument>.Update.Set("expiry", now.UtcTicks));

    /// <inheritdoc/>
    public async Task<IEnumerable<TriggerId>> GetPendingTriggers()
    {
        var documents = await _occurrences.Find(Builders<BsonDocument>.Filter.In("state", [(int)OccurrenceState.Pending, (int)OccurrenceState.Leased])).Project(Builders<BsonDocument>.Projection.Include("trigger")).ToListAsync();
        return documents.Select(document => new TriggerId(document["trigger"].AsString)).Distinct().ToArray();
    }
}
