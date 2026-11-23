// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a durable singleton in the physical MongoDB read-model database, shared by all aliases.
/// </summary>
/// <param name="database">The database used by the sink.</param>
public class SinkProvenanceStorage(IMongoDatabase database) : ISinkProvenanceStorage
{
    const string Marker = "protected";
    readonly IMongoCollection<BsonDocument> _collection = database
        .GetCollection<BsonDocument>(SinkProvenanceNames.Metadata)
        .WithReadPreference(ReadPreference.Primary)
        .WithReadConcern(ReadConcern.Majority)
        .WithWriteConcern(WriteConcern.WMajority);

    /// <inheritdoc/>
    public async Task<bool> IsQuarantined()
    {
        var records = await _collection.Find(FilterDefinition<BsonDocument>.Empty).Limit(2).ToListAsync();
        if (records.Count == 0) return false;
        if (records.Count != 1 || !IsValid(records[0]))
        {
            throw new InvalidOperationException("Read-model storage is not available.");
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task MarkQuarantined()
    {
        if (await IsQuarantined()) return;
        try
        {
            await _collection.UpdateOneAsync(
                new BsonDocument("_id", Marker),
                Builders<BsonDocument>.Update.SetOnInsert("restricted", true).SetOnInsert("version", 1),
                new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // A concurrent silo may have inserted the same singleton. Verify it before accepting success.
            if (!await IsQuarantined()) throw;
        }

        if (!await IsQuarantined()) throw new InvalidOperationException("Read-model storage is not available.");
    }

    static bool IsValid(BsonDocument record) =>
        record.GetValue("_id", BsonNull.Value) == Marker &&
        record.GetValue("version", BsonNull.Value) is BsonInt32 version && version.Value == 1 &&
        record.GetValue("restricted", BsonNull.Value) is BsonBoolean restricted && restricted.Value;
}
