// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_the_manager_is_cleared_with_pipelines_holding_sessions : given.a_pipeline_manager
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
        _manager.Clear();
        _found = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _binding);
    }

    [Fact] void should_keep_the_session_discoverable() => _found.ShouldBeTrue();
    [Fact] void should_return_the_pipeline_that_owns_the_session() => _binding.Pipeline.ShouldEqual(_pipeline);
    [Fact] void should_retain_no_current_pipelines() => _manager.PipelineCount.ShouldEqual(0);
}
