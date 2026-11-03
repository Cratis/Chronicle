// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_clearing_applied_event_markers;

public class and_the_sink_keeps_markers_for_a_marked_projection : given.a_pipeline_over_a_sink
{
    ISink _sink;
    ProjectionPipeline _pipeline;

    void Establish()
    {
        _sink = Substitute.For<ISink, ISinkAppliedEventMarkers>();
        _pipeline = CreatePipeline(_sink, _join);
    }

    async Task Because() => await _pipeline.ClearAppliedEventMarkers(_namespace, _partition);

    [Fact] async Task should_clear_the_markers_for_the_marked_joins() => await ((ISinkAppliedEventMarkers)_sink).Received(1).ClearAppliedEventMarkers(_namespace, _partition, Arg.Is<IReadOnlyList<AppliedEventMarkerJoin>>(joins => joins.SequenceEqual(new[] { _join })));
}
