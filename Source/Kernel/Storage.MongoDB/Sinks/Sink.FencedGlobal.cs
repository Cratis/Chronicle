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
                document = await result.SingleOrDefaultAsync();
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
    internal sealed record FencedGlobalState(string? Incarnation, long Revision, IReadOnlyDictionary<string, long> LastHandled)
    {
        /// <summary>
        /// Gets whether the instance did not exist when it was read.
        /// </summary>
        public bool IsAbsent => Incarnation is null;

        /// <summary>
        /// Gets the path of the last handled event sequence number for a writing namespace.
        /// </summary>
        /// <param name="writingNamespace">The namespace name.</param>
        /// <returns>The field path.</returns>
        public static string LastHandledPath(string writingNamespace) =>
            $"{WellKnownProperties.LastHandledByNamespace}.{EncodeNamespace(writingNamespace)}";

        /// <summary>
        /// Encodes a namespace as a field name. Escaping '%' first keeps it injective, so 'a.b' and 'a%2Eb' never share a key.
        /// </summary>
        /// <param name="writingNamespace">The namespace name.</param>
        /// <returns>The encoded field name.</returns>
        public static string EncodeNamespace(string writingNamespace) =>
            writingNamespace.Replace("%", "%25").Replace(".", "%2E").Replace("$", "%24");

        /// <summary>
        /// Reverses <see cref="EncodeNamespace"/> in a single pass.
        /// </summary>
        /// <param name="encoded">The encoded field name.</param>
        /// <returns>The namespace name.</returns>
        public static string DecodeNamespace(string encoded)
        {
            var decoded = new System.Text.StringBuilder(encoded.Length);
            var index = 0;
            while (index < encoded.Length)
            {
                var rest = encoded.AsSpan(index);
                if (rest.StartsWith("%25", StringComparison.Ordinal))
                {
                    decoded.Append('%');
                    index += 3;
                }
                else if (rest.StartsWith("%2E", StringComparison.Ordinal))
                {
                    decoded.Append('.');
                    index += 3;
                }
                else if (rest.StartsWith("%24", StringComparison.Ordinal))
                {
                    decoded.Append('$');
                    index += 3;
                }
                else
                {
                    decoded.Append(encoded[index]);
                    index++;
                }
            }

            return decoded.ToString();
        }

        /// <summary>
        /// Reads the bookkeeping from the initial state of a changeset.
        /// </summary>
        /// <param name="initialState">The state as read, if any.</param>
        /// <returns>The <see cref="FencedGlobalState"/>.</returns>
        public static FencedGlobalState From(ExpandoObject? initialState)
        {
            if (initialState is not IDictionary<string, object?> state || !state.TryGetValue(WellKnownProperties.Incarnation, out var incarnation) || incarnation is null)
            {
                return new(null, 0, new Dictionary<string, long>());
            }

            var revision = state.TryGetValue(WellKnownProperties.Revision, out var storedRevision) && storedRevision is not null ? Convert.ToInt64(storedRevision) : 0L;
            var lastHandled = new Dictionary<string, long>();
            if (state.TryGetValue(WellKnownProperties.LastHandledByNamespace, out var handled) && handled is IDictionary<string, object?> byNamespace)
            {
                foreach (var (name, value) in byNamespace.Where(_ => _.Value is not null))
                {
                    lastHandled[DecodeNamespace(name)] = Convert.ToInt64(value);
                }
            }

            return new(incarnation.ToString(), revision, lastHandled);
        }

        /// <summary>
        /// Only property changes are fenced; every other kind of change fails closed, so a new kind never writes unprotected.
        /// </summary>
        /// <param name="changeset">The changeset about to be written.</param>
        /// <exception cref="FencedWriteNotSupportedException">Thrown when the changeset holds a change that cannot be fenced.</exception>
        public static void EnsureSupported(IChangeset<AppendedEvent, ExpandoObject> changeset)
        {
            foreach (var change in changeset.Changes.Where(_ => _ is not PropertiesChanged<ExpandoObject>))
            {
                throw new FencedWriteNotSupportedException($"a change of kind '{change.GetType().Name}' is not supported");
            }
        }

        /// <summary>
        /// Gets whether the writing namespace already handled the event.
        /// </summary>
        /// <param name="writingNamespace">The namespace name.</param>
        /// <param name="eventSequenceNumber">The event sequence number.</param>
        /// <returns>True if handled, false otherwise.</returns>
        public bool HasHandled(string writingNamespace, EventSequenceNumber eventSequenceNumber) =>
            LastHandled.TryGetValue(writingNamespace, out var handled) && handled >= (long)eventSequenceNumber.Value;
    }
}
