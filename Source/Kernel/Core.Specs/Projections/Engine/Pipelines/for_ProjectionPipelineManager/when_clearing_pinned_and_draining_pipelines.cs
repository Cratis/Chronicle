// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_clearing_pinned_and_draining_pipelines : given.a_pipeline_manager
{
    IProjectionPipeline _draining;
    IProjectionPipeline _current;
    bool _drainingFound;
    bool _currentFound;
    bool _foundAfterSessionReopened;

    async Task Establish()
    {
        _draining = await _manager.GetFor(_store, _namespace, _projection);
        await _draining.BeginBulk();
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
        _current = await _manager.GetFor(_store, _namespace, _projection);
        await _current.BeginBulk();
    }

    async Task Because()
    {
        _manager.Clear();
        _drainingFound = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _);
        _currentFound = _drainingFound;
        await _draining.EndBulk();
        await _draining.BeginBulk();
        _foundAfterSessionReopened = _manager.TryGetFor(_store, _namespace, _projection.Identifier, out _);
    }

    [Fact] void should_retain_no_current_pipeline() => _manager.PipelineCount.ShouldEqual(0);
    [Fact] void should_retain_no_draining_pipeline() => _manager.DrainingCount.ShouldEqual(0);
    [Fact] void should_not_find_any_pipeline() => _currentFound.ShouldBeFalse();
    [Fact] void should_not_resurrect_a_discarded_pipeline_when_its_session_reopens() => _foundAfterSessionReopened.ShouldBeFalse();
}
