// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class Sink
{
    readonly HashSet<WriteModel<BsonDocument>> _guardedBulkUpserts = [];

    /// <inheritdoc/>
    public bool SupportsIsolatedReplace => true;

    /// <inheritdoc/>
    public async Task<IsolatedReplaceResult> ReplaceIsolated(Key key, ExpandoObject? state, EventSequenceNumber watermark)
    {
        // Flush accepted operations before replacing. Keep their outcome ledger for EndBulk, including
        // failures belonging to other keys. Evicting a cache alone would leave buffered Always writes alive.
        var drained = await DrainBulk();
        EvictDrainedCache(drained.Batches);
        var primary = collections.GetCollection(readModel.ContainerName);
        var id = Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));
        var filter = Builders<BsonDocument>.Filter.And(id, AtOrBelowWatermark(watermark));
        try
        {
            return await writes.InBatch(async () =>
            {
                if (state is null)
                {
                    var deleted = await writes.DeleteOne(primary, filter);
                    if (deleted.DeletedCount > 0 || !await primary.Find(id).AnyAsync())
                    {
                        writes.AfterCommit(() => EvictBulkState(key));
                        return IsolatedReplaceResult.Removed;
                    }
                    return IsolatedReplaceResult.Superseded;
                }

                var replacement = expandoObjectConverter.ToBsonDocument(state, readModel.GetSchemaForLatestGeneration());
                replacement["_id"] = converter.ToBsonValue(key);
                replacement[WellKnownProperties.LastHandledEventSequenceNumber] = converter.ToBsonValue(watermark);
                replacement[WellKnownProperties.ReadModelInstanceInitialized] = true;
                var operation = new ReplaceOneModel<BsonDocument>(filter, replacement) { IsUpsert = true };
                await writes.BulkWrite(primary, [operation], CancellationToken.None);
                writes.AfterCommit(() => EvictBulkState(key));
                return IsolatedReplaceResult.Replaced;
            });
        }
        catch (MongoBulkWriteException<BsonDocument> exception) when (
            exception.WriteConcernError is null && exception.WriteErrors.Count == 1 &&
            exception.WriteErrors[0].Category == ServerErrorCategory.DuplicateKey &&
            exception.WriteErrors[0].Message.Contains("index: _id_", StringComparison.Ordinal))
        {
            EvictBulkState(key);
            return IsolatedReplaceResult.Superseded;
        }
    }

    static bool IsDuplicateId(MongoWriteException exception) =>
        exception.WriteError.Category == ServerErrorCategory.DuplicateKey &&
        exception.WriteError.Message.Contains("index: _id_", StringComparison.Ordinal);

    FilterDefinition<BsonDocument> AtOrBelowWatermark(EventSequenceNumber watermark) =>
        Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Exists(WellKnownProperties.LastHandledEventSequenceNumber, false),
            Builders<BsonDocument>.Filter.Lte(WellKnownProperties.LastHandledEventSequenceNumber, converter.ToBsonValue(watermark)));
}
