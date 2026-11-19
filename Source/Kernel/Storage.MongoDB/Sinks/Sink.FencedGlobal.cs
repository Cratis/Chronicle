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
            BsonDocument? document;
            using (var result = await Collection.FindAsync(filter))
            {
                document = result.SingleOrDefault();
            }

            if (document is null)
            {
                return null;
            }

            var hasRevision = document.Contains(WellKnownProperties.Revision);
            var hasIncarnation = document.Contains(WellKnownProperties.Incarnation);
            if (hasRevision != hasIncarnation)
            {
                throw new InvalidOperationException($"The read model instance '{key}' has only one of '{WellKnownProperties.Revision}' and '{WellKnownProperties.Incarnation}', so it cannot be written safely");
            }

            if (hasRevision)
            {
                return expandoObjectConverter.ToExpandoObject(document, readModel.GetSchemaForLatestGeneration());
            }

            // A legacy instance gets an incarnation and revision 0 atomically, and is then read again, so the changes are always
            // computed from the stamped state. An instance deleted and recreated meanwhile is another incarnation and misses the write's filter.
            var legacyFilter = Builders<BsonDocument>.Filter.And(
                filter,
                Builders<BsonDocument>.Filter.Exists(WellKnownProperties.Revision, false),
                Builders<BsonDocument>.Filter.Exists(WellKnownProperties.Incarnation, false));
            var stamp = Builders<BsonDocument>.Update
                .Set(WellKnownProperties.Incarnation, Guid.NewGuid().ToString())
                .Set(WellKnownProperties.Revision, 0L);
            await writes.UpdateOne(Collection, legacyFilter, stamp, new UpdateOptions());
        }

        throw new SinkWriteConflictException(key);
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

        var converted = await changesetConverter.ToUpdateDefinition(key, changeset, eventSequenceNumber);
        if (!converted.hasChanges)
        {
            return [];
        }

        if (converted.NullParentPaths.Count > 0 || converted.NullArrayParents.Count > 0)
        {
            throw new FencedWriteNotSupportedException($"the changes for '{key}' need legacy null parents repaired, which is a separate write that cannot be fenced");
        }

        var idFilter = Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(key));
        var incarnation = expected.Incarnation ?? Guid.NewGuid().ToString();
        var filter = expected.IsAbsent
            ? Builders<BsonDocument>.Filter.And(idFilter, Builders<BsonDocument>.Filter.Eq(WellKnownProperties.Incarnation, incarnation))
            : Builders<BsonDocument>.Filter.And(idFilter, Builders<BsonDocument>.Filter.Eq(WellKnownProperties.Incarnation, incarnation), Builders<BsonDocument>.Filter.Eq(WellKnownProperties.Revision, expected.Revision));

        var update = Builders<BsonDocument>.Update.Combine(
            converted.UpdateDefinition,
            Builders<BsonDocument>.Update.Inc(WellKnownProperties.Revision, 1L),
            Builders<BsonDocument>.Update.Set(FencedGlobalState.LastHandledPath(writingNamespace), converter.ToBsonValue(eventSequenceNumber)));
        if (expected.IsAbsent)
        {
            update = Builders<BsonDocument>.Update.Combine(update, Builders<BsonDocument>.Update.Set(WellKnownProperties.Incarnation, incarnation));
        }

        var options = new UpdateOptions { IsUpsert = expected.IsAbsent, ArrayFilters = converted.ArrayFilters };
        try
        {
            var result = await writes.UpdateOne(Collection, filter, update, options);
            if (!expected.IsAbsent && result.MatchedCount == 0)
            {
                throw new SinkWriteConflictException(key);
            }
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == DuplicateKeyErrorCode)
        {
            throw new SinkWriteConflictException(key);
        }

        return [];
    }

    /// <summary>
    /// The bookkeeping a fenced write is checked against, as read with the instance, and the rules for what a fenced write may contain.
    /// </summary>
    /// <param name="Incarnation">The incarnation of the instance read, or null when there was none.</param>
    /// <param name="Revision">The revision of the instance read.</param>
    /// <param name="LastHandled">The last handled event sequence number per writing namespace.</param>
    sealed record FencedGlobalState(string? Incarnation, long Revision, IReadOnlyDictionary<string, long> LastHandled)
    {
        public bool IsAbsent => Incarnation is null;

        public static string LastHandledPath(string writingNamespace) =>
            $"{WellKnownProperties.LastHandledByNamespace}.{writingNamespace.Replace(".", "%2E").Replace("$", "%24")}";

        public static FencedGlobalState From(ExpandoObject? initialState)
        {
            var state = initialState as IDictionary<string, object?>;
            if (state is null || !state.TryGetValue(WellKnownProperties.Incarnation, out var incarnation) || incarnation is null)
            {
                return new(null, 0, new Dictionary<string, long>());
            }

            var revision = state.TryGetValue(WellKnownProperties.Revision, out var storedRevision) && storedRevision is not null ? Convert.ToInt64(storedRevision) : 0L;
            var lastHandled = new Dictionary<string, long>();
            if (state.TryGetValue(WellKnownProperties.LastHandledByNamespace, out var handled) && handled is IDictionary<string, object?> byNamespace)
            {
                foreach (var (name, value) in byNamespace.Where(_ => _.Value is not null))
                {
                    lastHandled[name.Replace("%2E", ".").Replace("%24", "$")] = Convert.ToInt64(value);
                }
            }

            return new(incarnation.ToString(), revision, lastHandled);
        }

        /// <summary>
        /// Only property changes are fenced; every other kind of change fails closed, so a new kind never writes unprotected.
        /// </summary>
        /// <param name="changeset">The changeset about to be written.</param>
        public static void EnsureSupported(IChangeset<AppendedEvent, ExpandoObject> changeset)
        {
            foreach (var change in changeset.Changes.Where(_ => _ is not PropertiesChanged<ExpandoObject>))
            {
                throw new FencedWriteNotSupportedException($"a change of kind '{change.GetType().Name}' is not supported");
            }
        }

        public bool HasHandled(string writingNamespace, EventSequenceNumber eventSequenceNumber) =>
            LastHandled.TryGetValue(writingNamespace, out var handled) && handled >= (long)eventSequenceNumber.Value;
    }
}
