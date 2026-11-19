// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_pipeline_with_an_open_bulk_session_is_evicted_and_recreated : given.a_pipeline_manager
{
    IProjectionPipeline _original;
    IProjectionPipeline _recreated;
    IProjectionPipeline _ownerWhileOpen;
    IProjectionPipeline _ownerAfterEnd;

    async Task Establish()
    {
        _original = await _manager.GetFor(_store, _namespace, _projection);
        await _original.BeginBulk();
    }

    async Task Because()
    {
        _manager.EvictFor(_store, _namespace, _projection.Identifier);
        _recreated = await _manager.GetFor(_store, _namespace, _projection);
        _manager.TryGetFor(_store, _namespace, _projection.Identifier, out var open);
        _ownerWhileOpen = open?.Pipeline;
        await _original.EndBulk();
        _manager.TryGetFor(_store, _namespace, _projection.Identifier, out var ended);
        _ownerAfterEnd = ended?.Pipeline;
    }

    [Fact] void should_build_a_different_pipeline() => _recreated.ShouldNotEqual(_original);
    [Fact] void should_not_begin_the_session_again() => _sink.Received(1).BeginBulk();
    [Fact] void should_keep_the_session_owner_discoverable_while_it_is_open() => _ownerWhileOpen.ShouldEqual(_original);
    [Fact] void should_return_the_recreated_pipeline_once_the_session_ended() => _ownerAfterEnd.ShouldEqual(_recreated);
    [Fact] void should_retain_one_current_pipeline() => _manager.PipelineCount.ShouldEqual(1);
    [Fact] void should_have_drained_the_session_owner() => _manager.DrainingCount.ShouldEqual(0);
}
