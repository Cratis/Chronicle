// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_sweeping_a_draining_pipeline : given.a_pipeline_manager
{
    IProjectionPipeline _pipeline;
    int _drainingWhileOpen;

    async Task Establish()
    {
        _pipeline = await _manager.GetFor(_store, _namespace, _projection);
        await _pipeline.BeginBulk();
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
    }

    async Task Because()
    {
        _manager.SweepIdle();
        _drainingWhileOpen = _manager.DrainingCount;
        await _pipeline.EndBulk();
        _manager.SweepIdle();
    }

    [Fact] void should_keep_it_while_the_session_is_open() => _drainingWhileOpen.ShouldEqual(1);
    [Fact] void should_prune_it_once_the_session_ended() => _manager.DrainingCount.ShouldEqual(0);
}
