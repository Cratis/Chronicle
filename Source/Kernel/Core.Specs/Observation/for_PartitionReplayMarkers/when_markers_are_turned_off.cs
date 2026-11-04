// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.for_PartitionReplayMarkers;

public class when_markers_are_turned_off : given.a_partition_replay_markers_service
{
    void Establish()
    {
        _projection.MarkedJoins.Returns([new AppliedEventMarkerJoin("def", "type", "customerId")]);
        _markers = new(_projections, _pipelineManager, Options.Create(new ChronicleOptions()));
    }

    async Task Because() => await _markers.ClearFor(new ObserverDetails(_observerKey, ObserverType.Projection), _partition);

    [Fact] async Task should_not_look_up_the_projection() => await _projections.DidNotReceive().GetCurrent(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
    [Fact] async Task should_not_clear_any_markers() => await _pipeline.DidNotReceive().ClearAppliedEventMarkers(Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>());
}
