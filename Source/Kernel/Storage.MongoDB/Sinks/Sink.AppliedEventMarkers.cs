// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class Sink : ISinkAppliedEventMarkers
{
    /// <inheritdoc/>
    public async Task ClearAppliedEventMarkers(EventStoreNamespaceName @namespace, Key partition, IReadOnlyList<AppliedEventMarkerJoin> joins)
    {
        DirectWrites.BeforeWrite();
        var markers = WellKnownProperties.AppliedEventMarkers;
        await writes.UpdateOne(
            Collection,
            Builders<BsonDocument>.Filter.Eq("_id", converter.ToBsonValue(partition)),
            Builders<BsonDocument>.Update.Unset($"{markers}.f"),
            new UpdateOptions());

        var stream = AppliedEventMarkers.StreamHash(@namespace.Value, partition.Value.ToString() ?? string.Empty);
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

    static UpdateWrite NewWrite(Key key, EventSequenceNumber eventSequenceNumber, UpdateDefinitionAndArrayFilters converted, WritePlan plan) =>
        converted.AppliedCondition is { } condition
            ? new(key, eventSequenceNumber, converted, Builders<BsonDocument>.Filter.And(plan.Filter, condition), plan.IsUpsert) { IdFilter = plan.Filter }
            : new(key, eventSequenceNumber, converted, plan.Filter, plan.IsUpsert);

    AppliedEventMarkerContext? MarkersFor(IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, WritePlan plan, SinkWriteOptions options) =>
        options.Mode == SinkWriteMode.OncePerSourceEvent && changeset.Incoming is { } incoming && eventSequenceNumber.IsActualValue && changesetConverter is IChangesetConverterWithDirectWrites
            ? AppliedEventMarkerContext.For(incoming, !IsWrittenThroughJoinTargets(plan))
            : null;

    FilterDefinition<BsonDocument> WithoutRepeatedRemoval(FilterDefinition<BsonDocument> filter, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, WritePlan plan, SinkWriteOptions options)
    {
        var markers = MarkersFor(changeset, eventSequenceNumber, plan, options);
        return markers is null
            ? filter
            : Builders<BsonDocument>.Filter.And(filter, AppliedEventMarkerSlots.NotApplied(AppliedEventMarkerSlots.Own("0"), markers.OwnStream, markers.SequenceNumber));
    }

    async Task RecordResolvedJoinSlots(UpdateWrite write, UpdateDefinitionAndArrayFilters converted, WritePlan plan, SinkWriteOptions options)
    {
        if (IsWrittenThroughJoinTargets(plan))
        {
            return;
        }

        foreach (var slot in converted.ResolvedJoinSlots)
        {
            var path = AppliedEventMarkerSlots.Join(slot.DefinitionId);
            var filter = Builders<BsonDocument>.Filter.And(plan.Filter, AppliedEventMarkerSlots.NotApplied(path, slot.Stream, slot.SequenceNumber));
            var update = Builders<BsonDocument>.Update.Set(path, AppliedEventMarkerSlots.Value(slot.Stream, slot.SequenceNumber));
            if (!await TryBufferOperation(new UpdateOneModel<BsonDocument>(filter, update), write, options))
            {
                await writes.UpdateOne(Collection, filter, update, new UpdateOptions());
            }
        }
    }
}
