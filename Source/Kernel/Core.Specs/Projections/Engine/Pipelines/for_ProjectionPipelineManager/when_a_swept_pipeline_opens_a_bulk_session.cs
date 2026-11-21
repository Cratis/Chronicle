// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_swept_pipeline_opens_a_bulk_session : given.a_pipeline_manager
{
    IProjectionPipeline _pipeline;
    bool _releasedBefore;
    ProjectionPipelineBinding _binding;

    async Task Establish()
    {
        _pipeline = await _manager.GetFor(_store, _namespace, _projection);
        _manager.SweepIdle();
        _time.Advance(ProjectionPipelineManager.IdleTimeout + TimeSpan.FromMinutes(1));
        _manager.SweepIdle();
        _releasedBefore = !_manager.TryGetFor(_store, _namespace, _projection.Identifier, out _);
    }

    async Task Because()
    {
        await _pipeline.BeginBulk();
        _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _binding);
    }

    [Fact] void should_have_been_released_before() => _releasedBefore.ShouldBeTrue();
    [Fact] void should_be_discoverable_again_for_flush() => _binding.Pipeline.ShouldEqual(_pipeline);
}
