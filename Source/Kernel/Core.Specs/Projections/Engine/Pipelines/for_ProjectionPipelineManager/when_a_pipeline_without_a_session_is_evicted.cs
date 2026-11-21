// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_pipeline_without_a_session_is_evicted : given.a_pipeline_manager
{
    bool _found;

    async Task Establish() => await _manager.GetFor(_store, _namespace, _projection);

    void Because()
    {
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
        _found = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _);
    }

    [Fact] void should_no_longer_find_the_pipeline() => _found.ShouldBeFalse();
    [Fact] void should_not_retain_it_as_draining() => _manager.DrainingCount.ShouldEqual(0);
    [Fact] void should_retain_no_pipelines() => _manager.PipelineCount.ShouldEqual(0);
}
