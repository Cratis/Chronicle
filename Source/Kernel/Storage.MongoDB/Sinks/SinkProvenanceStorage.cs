// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Stores monotonic inventory facts in the physical read-model database.
/// </summary>
/// <param name="database">The database used by the sink.</param>
public class SinkProvenanceStorage(IMongoDatabase database) : ISinkProvenanceStorage
{
    const string Marker = "protected";
    const string State = "inventory";
    readonly IMongoCollection<BsonDocument> _collection = database
        .GetCollection<BsonDocument>(SinkProvenanceNames.Metadata)
        .WithReadPreference(ReadPreference.Primary)
        .WithReadConcern(ReadConcern.Majority)
        .WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<SinkProvenanceState> ReadState()
    {
        var (state, marked) = await Read();
        if (state is null)
        {
            await Bootstrap();
            (state, marked) = await Read();
        }
        if (state is null) throw Unavailable();
        if (marked && !state.Restricted)
        {
            await _collection.UpdateOneAsync(new BsonDocument("_id", State), Builders<BsonDocument>.Update.Set("restricted", true));
            state = state with { Restricted = true };
        }
        return state.Validate();
    }

    /// <inheritdoc/>
    public async Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence)
    {
        var state = await ReadState();
        if (state.IsComplete) return state;
        var update = Builders<BsonDocument>.Update.Set("inventoryVersion", SinkProvenanceState.CurrentInventoryVersion);
        if (restrictedEvidence || state.Restricted) update = update.Set("restricted", true);
        await _collection.UpdateOneAsync(
            new BsonDocument { { "_id", State }, { "version", 1 }, { "inventoryVersion", 0 } }, update);
        var committed = await ReadState();
        if (!committed.IsComplete) throw Unavailable();
        return committed;
    }

    /// <inheritdoc/>
    public async Task<bool> IsQuarantined() => (await ReadState()).Restricted;

    /// <inheritdoc/>
    public async Task MarkQuarantined()
    {
        await ReadState();
        await _collection.UpdateOneAsync(new BsonDocument("_id", State), Builders<BsonDocument>.Update.Set("restricted", true));
        try
        {
            await _collection.UpdateOneAsync(
                new BsonDocument("_id", Marker),
                Builders<BsonDocument>.Update.SetOnInsert("restricted", true).SetOnInsert("version", 1),
                new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // The read-back validates the concurrently committed marker.
        }
        if (!(await ReadState()).Restricted) throw Unavailable();
    }

    static SinkProvenanceState Parse(BsonDocument record)
    {
        if (record.GetValue("version", BsonNull.Value) is not BsonInt32 { Value: 1 } ||
            record.GetValue("inventoryVersion", BsonNull.Value) is not BsonInt32 inventory ||
            record.GetValue("restricted", BsonNull.Value) is not BsonBoolean restricted ||
            record.GetValue("databaseId", BsonNull.Value) is not BsonString id || !Guid.TryParseExact(id.Value, "D", out var databaseId))
        {
            throw Unavailable();
        }
        return new SinkProvenanceState(new(databaseId), inventory.Value, restricted.Value).Validate();
    }

    static bool IsMarker(BsonDocument record) =>
        record.GetValue("version", BsonNull.Value) is BsonInt32 { Value: 1 } &&
        record.GetValue("restricted", BsonNull.Value) is BsonBoolean { Value: true };

    static InvalidOperationException Unavailable() => new("Read-model storage is not available.");

    async Task Bootstrap()
    {
        try
        {
            await _collection.UpdateOneAsync(
                new BsonDocument("_id", State),
                Builders<BsonDocument>.Update.SetOnInsert("databaseId", Guid.NewGuid().ToString("D"))
                    .SetOnInsert("version", 1).SetOnInsert("inventoryVersion", 0).SetOnInsert("restricted", false),
                new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // The committed winner is validated by the caller's read-back.
        }
    }

    async Task<(SinkProvenanceState? State, bool Marked)> Read()
    {
        var records = await _collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(3).ToListAsync();
        SinkProvenanceState? state = null;
        var marked = false;
        foreach (var record in records)
        {
            var id = record.GetValue("_id", BsonNull.Value);
            if (id == Marker && !marked && IsMarker(record)) marked = true;
            else if (id == State && state is null) state = Parse(record);
            else throw Unavailable();
        }
        return (state, marked);
    }
}
