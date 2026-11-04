// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildFutures.given;

public class partition_rebuild_futures : Specification
{
    internal PartitionRebuildFutures _futures;

    void Establish() => _futures = new();

    protected static ProjectionFuture Future(EventSequenceNumber sequenceNumber, EventTypeGeneration? generation = null) => new(
        new ProjectionFutureId(Guid.NewGuid()),
        new ProjectionId("projection"),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("Event", generation ?? EventTypeGeneration.First), sequenceNumber),
        PropertyPath.NotSet,
        new PropertyPath("children"),
        PropertyPath.NotSet,
        PropertyPath.NotSet,
        new Key("partition", ArrayIndexers.NoIndexers),
        DateTimeOffset.UtcNow);
}
