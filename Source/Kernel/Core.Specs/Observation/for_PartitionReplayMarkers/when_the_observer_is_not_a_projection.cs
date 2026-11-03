// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_PartitionReplayMarkers;

public class when_the_observer_is_not_a_projection : given.a_partition_replay_markers_service
{
    void Establish()
    {
        _projection.MarkedJoins.Returns([new AppliedEventMarkerJoin("def", "type", "customerId")]);
    }

    async Task Because() => await _markers.ClearFor(new ObserverDetails(_observerKey, ObserverType.Reactor), _partition);

    [Fact] async Task should_not_clear_any_markers() => await _pipeline.DidNotReceive().ClearAppliedEventMarkers(Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>());
}
