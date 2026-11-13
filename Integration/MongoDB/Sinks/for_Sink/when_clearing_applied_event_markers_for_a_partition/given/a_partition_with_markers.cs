// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_clearing_applied_event_markers_for_a_partition.given;

public class a_partition_with_markers(ChronicleInProcessFixture fixture) : when_redelivering_to_a_source_keyed_join_projection.given.a_source_keyed_join_projection(fixture)
{
    protected static readonly string DefinitionId = AppliedEventMarkers.DefinitionId("joined", "joinId");
    protected static readonly Cratis.Chronicle.Concepts.EventStoreNamespaceName Namespace = "default";

    protected static IReadOnlyList<AppliedEventMarkerJoin> Joins => [new(DefinitionId, "joined", "joinId")];

    protected Task Clear(Sink sink, string partition) =>
        (sink as ISinkAppliedEventMarkers ?? throw new InvalidOperationException("The sink does not clear applied event markers")).ClearAppliedEventMarkers(Namespace, Cratis.Chronicle.Concepts.EventSequences.EventSequenceId.Log, partition, Joins);

    protected static bool HasJoinSlot(BsonDocument document) =>
        document.TryGetValue(Applied, out var applied) && applied.AsBsonDocument.TryGetValue("j", out var joins) && joins.AsBsonDocument.Contains(DefinitionId);

    protected static bool HasOwnSlot(BsonDocument document) =>
        document.TryGetValue(Applied, out var applied) && applied.AsBsonDocument.TryGetValue("f", out var own) && own.AsBsonDocument.ElementCount > 0;
}
