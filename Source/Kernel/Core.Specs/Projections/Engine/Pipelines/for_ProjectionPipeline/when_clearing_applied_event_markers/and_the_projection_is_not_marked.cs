// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_clearing_applied_event_markers;

public class and_the_projection_is_not_marked : given.a_pipeline_over_a_sink
{
    ISink _sink;
    ProjectionPipeline _pipeline;

    void Establish()
    {
        _sink = Substitute.For<ISink, ISinkAppliedEventMarkers>();
        _pipeline = CreatePipeline(_sink);
    }

    async Task Because() => await _pipeline.ClearAppliedEventMarkers(_namespace, _partition);

    [Fact] async Task should_not_clear_any_markers() => await ((ISinkAppliedEventMarkers)_sink).DidNotReceive().ClearAppliedEventMarkers(Arg.Any<EventStoreNamespaceName>(), Arg.Any<Key>(), Arg.Any<IReadOnlyList<AppliedEventMarkerJoin>>());
}
