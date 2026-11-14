// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class Sink : ISinkAppliedEventMarkers
{
    /// <inheritdoc/>
    public async Task ClearAppliedEventMarkers(EventStoreNamespaceName @namespace, EventSequenceId eventSequence, Key partition, IReadOnlyList<AppliedEventMarkerJoin> joins)
    {
        DirectWrites.BeforeWrite();
        const string markers = WellKnownProperties.AppliedEventMarkers;
        await writes.UpdateOne(
            Collection,
            Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(partition)),
            Builders<BsonDocument>.Update.Unset($"{markers}.f"),
            new UpdateOptions());

        var stream = AppliedEventMarkers.StreamHash(@namespace.Value, eventSequence.Value, partition.Value.ToString() ?? string.Empty);
        foreach (var join in joins)
        {
            var slot = AppliedEventMarkerSlots.Join(join.DefId);
            await writes.UpdateMany(
                Collection,
                Builders<BsonDocument>.Filter.Eq($"{slot}.s", stream),
                Builders<BsonDocument>.Update.Unset(slot),
                new UpdateOptions());
        }

        writes.AfterCommit(() => EvictBulkState(partition));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChangesOncePerSourceEvent(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        AppliedEventMarkerWriteOrigin origin)
    {
        var options = new SinkWriteOptions(SinkWriteMode.OncePerSourceEvent, EventSequence: origin.EventSequence.Value);
        return origin.Immediately
            ? ApplyChangesImmediately(key, changeset, eventSequenceNumber, options)
            : ApplyChangesCore(key, changeset, eventSequenceNumber, options);
    }

    static UpdateWrite NewWrite(Key key, EventSequenceNumber eventSequenceNumber, UpdateDefinitionAndArrayFilters converted, WritePlan plan) =>
        new(key, eventSequenceNumber, converted, plan.Filter, plan.IsUpsert);

    AppliedEventMarkerContext? MarkersFor(IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, WritePlan plan, SinkWriteOptions options) =>
        options.Mode == SinkWriteMode.OncePerSourceEvent && changeset.Incoming is { } incoming && eventSequenceNumber.IsActualValue && changesetConverter is IChangesetConverterWithDirectWrites
            ? AppliedEventMarkerContext.For(incoming, options.EventSequence, !IsWrittenThroughJoinTargets(plan))
            : null;

    FilterDefinition<BsonDocument> WithoutRepeatedRemoval(FilterDefinition<BsonDocument> filter, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, WritePlan plan, SinkWriteOptions options)
    {
        var markers = MarkersFor(changeset, eventSequenceNumber, plan, options);
        return markers is null
            ? filter
            : Builders<BsonDocument>.Filter.And(filter, AppliedEventMarkerSlots.NotApplied(AppliedEventMarkerSlots.Own("0"), markers.OwnStream, markers.SequenceNumber));
    }
}
