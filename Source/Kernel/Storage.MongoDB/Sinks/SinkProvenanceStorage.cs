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
    public async Task<bool> IsQuarantined() =>
        await _collection.CountDocumentsAsync(new BsonDocument("_id", Marker)) != 0;

    /// <inheritdoc/>
    public async Task MarkQuarantined()
    {
        try
        {
            await _collection.UpdateOneAsync(
                new BsonDocument("_id", Marker),
                Builders<BsonDocument>.Update.SetOnInsert("protected", true),
                new UpdateOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // A concurrent silo may have inserted the same singleton. Verify it before accepting success.
            if (!await IsQuarantined()) throw;
        }
    }
}
