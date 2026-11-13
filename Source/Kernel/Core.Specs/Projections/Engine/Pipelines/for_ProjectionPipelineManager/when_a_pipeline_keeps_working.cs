// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_pipeline_keeps_working : given.a_pipeline_manager
{
    IProjectionPipeline _pipeline;
    IProjectionPipeline _resolved;

    async Task Establish()
    {
        _pipeline = await _manager.GetFor(_store, _namespace, _projection);
        _manager.SweepIdle();
    }

    async Task Because()
    {
        for (var i = 0; i < 4; i++)
        {
            _time.Advance(ProjectionPipelineManager.IdleTimeout / 2);
            await _pipeline.BeginBulk();
            _manager.SweepIdle();
        }

        _resolved = await _manager.GetFor(_store, _namespace, _projection);
    }

    [Fact] void should_keep_the_pipeline() => _manager.PipelineCount.ShouldEqual(1);
    [Fact] void should_hand_out_the_same_pipeline() => _resolved.ShouldEqual(_pipeline);
}
