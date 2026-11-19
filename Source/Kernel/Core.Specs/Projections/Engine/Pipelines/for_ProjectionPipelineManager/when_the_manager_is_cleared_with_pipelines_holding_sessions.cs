// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_the_manager_is_cleared_with_pipelines_holding_sessions : given.a_pipeline_manager
{
    bool _found;

    async Task Establish()
    {
        var pipeline = await _manager.GetFor(_store, _namespace, _projection);
        await pipeline.BeginBulk();
    }

    void Because()
    {
        _manager.Clear();
        _found = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _);
    }

    [Fact] void should_not_keep_the_session_discoverable() => _found.ShouldBeFalse();
    [Fact] void should_retain_no_current_pipelines() => _manager.PipelineCount.ShouldEqual(0);
}
