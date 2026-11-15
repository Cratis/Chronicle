// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_an_idle_pipeline_is_replaced_while_the_old_one_is_still_held : given.a_pipeline_manager
{
    IProjectionPipeline _old;
    IProjectionPipeline _new;

    async Task Establish()
    {
        _old = await _manager.GetFor(_store, _namespace, _projection);
        _manager.SweepIdle();
        _time.Advance(ProjectionPipelineManager.IdleTimeout + TimeSpan.FromMinutes(1));
        _manager.SweepIdle();
    }

    async Task Because() => _new = await _manager.GetFor(_store, _namespace, _projection);

    [Fact] void should_have_released_the_old_pipeline() => _new.ShouldNotEqual(_old);
    [Fact] void should_share_one_handle_lock_between_old_and_new() => _manager.HandleLockCount.ShouldEqual(1);
    [Fact] void should_keep_the_old_pipeline_alive() => GC.KeepAlive(_old);
}
