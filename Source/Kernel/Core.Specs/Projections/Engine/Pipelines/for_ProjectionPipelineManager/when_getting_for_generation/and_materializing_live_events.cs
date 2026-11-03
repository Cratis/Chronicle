// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class and_materializing_live_events : given.a_materializing_pipeline_manager
{
    IProjectionPipeline _pipeline;
    IProjectionPipeline _cached;

    async Task Because()
    {
        _pipeline = await _manager.GetFor(_store, _namespace, _projection, _generation);
        await _pipeline.BeginReplay(_replay);
        await _pipeline.Handle(Incoming(_first, "generation", 1));
        await _pipeline.EndReplay(_replay);
        _cached = await _manager.GetFor(_store, _namespace, _projection, _generation);
    }

    [Fact] void should_cache_the_generation_pipeline() => _cached.ShouldBeSame(_pipeline);
    [Fact] void should_resolve_futures_under_the_generation_observer() => _grainFactory.Received(1).GetGrain<IProjectionFutures>(new ProjectionFuturesKey(_generation.ObserverId.Value, _store, _namespace).ToString(), null);
    [Fact] async Task should_select_the_generation_sink_once() => await _namespaceStorage.Sinks.Received(1).GetFor(_readModel, _generation.Destination);
    [Fact] async Task should_store_changesets_under_the_generation_destination() => await _sharedChangesets.Received(1).Save(_generation.Destination, Arg.Any<Key>(), Arg.Any<EventType>(), (EventSequenceNumber)1, Arg.Any<CorrelationId>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>());
    [Fact] async Task should_write_to_the_generation_sink() => await _generationSink.Received(1).ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), (EventSequenceNumber)1, Arg.Any<SinkWriteMode>());
    [Fact] async Task should_not_begin_shared_replay_for_a_generation() => await _sharedChangesets.DidNotReceive().BeginReplay(Arg.Any<ReadModelContainerName>());
    [Fact] async Task should_not_end_shared_replay_for_a_generation() => await _sharedChangesets.DidNotReceive().EndReplay(Arg.Any<ReadModelContainerName>());
    [Fact] async Task should_not_put_the_generation_sink_in_replay_mode() => await _generationSink.DidNotReceive().BeginReplay(Arg.Any<ReplayContext>());
    [Fact] void should_not_touch_the_primary_sink() => _liveSink.ReceivedCalls().ShouldBeEmpty();
}
