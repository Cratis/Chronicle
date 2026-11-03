// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_clearing_applied_event_markers;

public class and_the_sink_keeps_no_markers : given.a_pipeline_over_a_sink
{
    ProjectionPipeline _pipeline;
    Exception _error;

    void Establish() => _pipeline = CreatePipeline(Substitute.For<ISink>(), _join);

    async Task Because() => _error = await Catch.Exception(() => _pipeline.ClearAppliedEventMarkers(_namespace, _partition));

    [Fact] void should_do_nothing() => _error.ShouldBeNull();
}
