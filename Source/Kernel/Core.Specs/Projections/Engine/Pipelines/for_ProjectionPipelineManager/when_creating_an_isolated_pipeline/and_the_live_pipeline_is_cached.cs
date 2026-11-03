// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_creating_an_isolated_pipeline;

public class and_the_live_pipeline_is_cached : given.a_pipeline_manager
{
    IProjectionPipeline _live;
    IProjectionPipeline _isolated;
    IProjectionPipeline _liveAfter;
    ISink _isolatedSink;

    async Task Establish()
    {
        _isolatedSink = Substitute.For<ISink>();
        _live = await _manager.GetFor(_store, _namespace, _projection);
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.ClearReceivedCalls();
    }

    async Task Because()
    {
        _isolated = _manager.CreateIsolated(_store, _namespace, _projection, _isolatedSink);
        _liveAfter = await _manager.GetFor(_store, _namespace, _projection);
        await _isolated.BeginBulk();
    }

    [Fact] void should_create_a_pipeline() => _isolated.ShouldNotBeNull();
    [Fact] void should_not_be_the_live_pipeline() => _isolated.ShouldNotBeSame(_live);
    [Fact] async Task should_use_the_supplied_sink() => await _isolatedSink.Received(1).BeginBulk();
    [Fact] async Task should_not_touch_the_live_sink() => await _sink.DidNotReceive().BeginBulk();
    [Fact] void should_not_evict_the_live_pipeline() => _liveAfter.ShouldBeSame(_live);
    [Fact] async Task should_not_get_a_sink_from_sinks() => await _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.DidNotReceive().GetFor(Arg.Any<Concepts.ReadModels.ReadModelDefinition>());
}
