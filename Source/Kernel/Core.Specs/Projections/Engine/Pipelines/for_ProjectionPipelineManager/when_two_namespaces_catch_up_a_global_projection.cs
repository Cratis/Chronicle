// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_two_namespaces_catch_up_a_global_projection : given.a_pipeline_manager
{
    IProjectionPipeline _first;
    IProjectionPipeline _second;

    async Task Establish()
    {
        _projection.Scope.Returns(ProjectionScope.Global);
        _storage.GetEventStore(_store).GetNamespace(EventStoreNamespaceName.NotSet).Sinks.GetFor(_projection.ReadModel).Returns(_sink);
        _first = await _manager.GetFor(_store, "first", _projection);
        _second = await _manager.GetFor(_store, "second", _projection);
    }

    async Task Because()
    {
        await _first.BeginBulk();
        await _second.BeginBulk();
        await _first.FlushBulk();
        await _first.EndBulk();
    }

    [Fact] void should_not_begin_a_window_on_the_shared_sink() => _sink.DidNotReceive().BeginBulk();
    [Fact] void should_not_flush_the_shared_sink() => _sink.DidNotReceive().FlushBulk();
    [Fact] void should_not_end_a_window_on_the_shared_sink() => _sink.DidNotReceive().EndBulk();
}
