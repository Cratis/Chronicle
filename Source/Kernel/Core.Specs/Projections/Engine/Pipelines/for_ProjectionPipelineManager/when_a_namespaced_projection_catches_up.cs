// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_namespaced_projection_catches_up : given.a_pipeline_manager
{
    IProjectionPipeline _pipeline;

    async Task Establish()
    {
        _projection.Scope.Returns(ProjectionScope.Namespaced);
        _pipeline = await _manager.GetFor(_store, _namespace, _projection);
    }

    async Task Because()
    {
        await _pipeline.BeginBulk();
        await _pipeline.ResumeBulk();
        await _pipeline.FlushBulk();
        await _pipeline.EndBulk();
    }

    [Fact] void should_begin_a_window_on_the_sink() => _sink.Received(1).BeginBulk();
    [Fact] void should_resume_a_window_on_the_sink() => _sink.Received(1).ResumeBulk();
    [Fact] void should_flush_the_sink() => _sink.Received(1).FlushBulk();
    [Fact] void should_end_the_window_on_the_sink() => _sink.Received(1).EndBulk();
}
