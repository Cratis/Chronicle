// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_many_namespaces_were_active_and_then_idle : given.a_pipeline_manager
{
    const int Namespaces = 200;
    int _before;

    async Task Establish()
    {
        for (var i = 0; i < Namespaces; i++) await _manager.GetFor(_store, $"ns-{i}", _projection);
        _before = _manager.PipelineCount;
    }

    async Task Because()
    {
        _manager.SweepIdle();
        _time.Advance(ProjectionPipelineManager.IdleTimeout + TimeSpan.FromMinutes(1));
        _manager.SweepIdle();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        _manager.SweepIdle();
        await Task.CompletedTask;
    }

    [Fact] void should_have_retained_every_pipeline_while_active() => _before.ShouldEqual(Namespaces);
    [Fact] void should_release_every_idle_pipeline() => _manager.PipelineCount.ShouldEqual(0);
    [Fact] void should_release_the_handle_lock_keys() => _manager.HandleLockCount.ShouldBeLessThan(Namespaces);
}
