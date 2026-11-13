// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_PartitionReplayMarkers;

public class when_the_projection_is_marked : given.a_partition_replay_markers_service
{
    void Establish()
    {
        _projection.MarkedJoins.Returns([new AppliedEventMarkerJoin("def", "type", "customerId")]);
    }

    async Task Because() => await _markers.ClearFor(new ObserverDetails(_observerKey, ObserverType.Projection), _partition);

    [Fact] async Task should_clear_the_markers_of_the_partition() => await _pipeline.Received(1).ClearAppliedEventMarkers(_observerKey.Namespace, _partition);
}
