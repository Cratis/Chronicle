// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The <see cref="SinkWriteMode.FencedGlobal"/> part of the <see cref="Sink"/>: optimistic revision checks for an instance several namespaces write.
/// </summary>
public partial class Sink
{
    const int MaxStampAttempts = 5;
    const int DuplicateKeyErrorCode = 11000;

    /// <inheritdoc/>
    public async Task<ExpandoObject?> FindOrDefault(Key key, SinkWriteMode mode)
    {
        if (mode != SinkWriteMode.FencedGlobal)
        {
            return await FindOrDefault(key);
        }

        var filter = Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));
        for (var attempt = 0; attempt < MaxStampAttempts; attempt++)
        {
            var document = await FindDocument(filter);
            if (document is null)
            {
                return null;
            }

            if (IsStamped(document, key))
            {
                return expandoObjectConverter.ToExpandoObject(document, readModel.GetSchemaForLatestGeneration());
            }

            await StampLegacyInstance(filter);
        }

        throw new SinkWriteConflictException(key);
    }

    static bool IsStamped(BsonDocument document, Key key)
    {
        var hasRevision = document.Contains(WellKnownProperties.Revision);
        if (hasRevision != document.Contains(WellKnownProperties.Incarnation))
        {
            throw new InvalidOperationException($"The read model instance '{key}' has only one of '{WellKnownProperties.Revision}' and '{WellKnownProperties.Incarnation}', so it cannot be written safely");
        }

        return hasRevision;
    }

    static FilterDefinition<BsonDocument> IncarnationAndRevision(FencedGlobalState expected, string incarnation)
    {
        var incarnationFilter = Builders<BsonDocument>.Filter.Eq(WellKnownProperties.Incarnation, incarnation);
        return expected.IsAbsent
            ? incarnationFilter
            : Builders<BsonDocument>.Filter.And(incarnationFilter, Builders<BsonDocument>.Filter.Eq(WellKnownProperties.Revision, expected.Revision));
    }

    async Task<BsonDocument?> FindDocument(FilterDefinition<BsonDocument> filter)
    {
        using var result = await Collection.FindAsync(filter);
        return await result.SingleOrDefaultAsync();
    }

    /// <summary>
    /// Gives a legacy instance an incarnation and revision 0 atomically; it is then read again, so the changes are always computed from the stamped state.
    /// An instance deleted and recreated meanwhile is another incarnation and misses the write's filter.
    /// </summary>
    /// <param name="filter">The filter selecting the instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    async Task StampLegacyInstance(FilterDefinition<BsonDocument> filter)
    {
        var legacyFilter = Builders<BsonDocument>.Filter.And(
            filter,
            Builders<BsonDocument>.Filter.Exists(WellKnownProperties.Revision, false),
            Builders<BsonDocument>.Filter.Exists(WellKnownProperties.Incarnation, false));
        var stamp = Builders<BsonDocument>.Update
            .Set(WellKnownProperties.Incarnation, Guid.NewGuid().ToString())
            .Set(WellKnownProperties.Revision, 0L);
        await writes.UpdateOne(Collection, legacyFilter, stamp, new UpdateOptions());
    }

    async Task<IEnumerable<FailedPartition>> ApplyFencedGlobal(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber)
    {
        FencedGlobalState.EnsureSupported(changeset);

        var writingNamespace = changeset.Incoming.Context.Namespace.Value;
        var expected = FencedGlobalState.From(changeset.InitialState);
        if (eventSequenceNumber.IsActualValue && expected.HasHandled(writingNamespace, eventSequenceNumber))
        {
            return [];
        }

        // The scalar watermark is not written: the per-namespace map in the same update replaces it, and a shared scalar would mix namespaces.
        var converted = await changesetConverter.ToUpdateDefinition(key, changeset, EventSequenceNumber.Unavailable);
        if (!converted.hasChanges)
        {
            return [];
        }

        if (converted.NullParentPaths.Count > 0 || converted.NullArrayParents.Count > 0)
        {
            throw new FencedWriteNotSupportedException($"the changes for '{key}' need legacy null parents repaired, which is a separate write that cannot be fenced");
        }

        var incarnation = expected.Incarnation ?? Guid.NewGuid().ToString();
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key)),
            IncarnationAndRevision(expected, incarnation));
        var update = BuildFencedUpdate(converted.UpdateDefinition, expected, incarnation, writingNamespace, eventSequenceNumber);
        var options = new UpdateOptions { IsUpsert = expected.IsAbsent, ArrayFilters = converted.ArrayFilters };
        await WriteFenced(key, filter, update, options, expected.IsAbsent);
        return [];
    }

    UpdateDefinition<BsonDocument> BuildFencedUpdate(UpdateDefinition<BsonDocument> changes, FencedGlobalState expected, string incarnation, string writingNamespace, EventSequenceNumber eventSequenceNumber)
    {
        var update = Builders<BsonDocument>.Update.Combine(
            changes,
            Builders<BsonDocument>.Update.Inc(WellKnownProperties.Revision, 1L),
            Builders<BsonDocument>.Update.Set(FencedGlobalState.LastHandledPath(writingNamespace), converter.ToBsonValue(eventSequenceNumber)));
        return expected.IsAbsent
            ? Builders<BsonDocument>.Update.Combine(update, Builders<BsonDocument>.Update.Set(WellKnownProperties.Incarnation, incarnation))
            : update;
    }

    async Task WriteFenced(Key key, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options, bool isCreate)
    {
        try
        {
            var result = await writes.UpdateOne(Collection, filter, update, options);
            if (!isCreate && result.MatchedCount == 0)
            {
                throw new SinkWriteConflictException(key);
            }
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == DuplicateKeyErrorCode)
        {
            throw new SinkWriteConflictException(key);
        }
    }
}
