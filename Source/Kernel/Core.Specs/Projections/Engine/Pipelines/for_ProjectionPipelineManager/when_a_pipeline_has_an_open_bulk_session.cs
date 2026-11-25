// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_pipeline_has_an_open_bulk_session : given.a_pipeline_manager
{
    ProjectionPipelineBinding _binding;

    async Task Establish()
    {
        var pipeline = await _manager.GetFor(_store, _namespace, _projection);
        await pipeline.BeginBulk();
        _manager.SweepIdle();
    }

    void Because()
    {
        _time.Advance(ProjectionPipelineManager.IdleTimeout * 3);
        _manager.SweepIdle();
        _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _binding);
    }

    [Fact] void should_keep_the_pipeline_discoverable_for_flush() => _binding.ShouldNotBeNull();
}
