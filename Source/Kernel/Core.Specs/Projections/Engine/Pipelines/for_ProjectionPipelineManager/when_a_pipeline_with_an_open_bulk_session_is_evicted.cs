// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_pipeline_with_an_open_bulk_session_is_evicted : given.a_pipeline_manager
{
    IProjectionPipeline _pipeline;
    ProjectionPipelineBinding _binding;
    bool _found;

    async Task Establish()
    {
        _pipeline = await _manager.GetFor(_store, _namespace, _projection);
        await _pipeline.BeginBulk();
    }

    void Because()
    {
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
        _found = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _binding);
    }

    [Fact] void should_keep_the_session_discoverable_for_flush() => _found.ShouldBeTrue();
    [Fact] void should_return_the_pipeline_that_owns_the_session() => _binding.Pipeline.ShouldEqual(_pipeline);
    [Fact] void should_not_keep_it_as_a_current_pipeline() => _manager.PipelineCount.ShouldEqual(0);
}
