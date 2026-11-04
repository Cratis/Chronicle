// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_live_delivery;

public class and_an_isolated_pipeline_was_used : given.a_materializing_pipeline_manager
{
    IProjectionPipeline _live;
    IProjectionPipeline _cached;

    async Task Establish()
    {
        var sink = new PartitionRebuildSink(new Key("source", ArrayIndexers.NoIndexers));
        var isolated = _manager.CreateIsolated(_store, _namespace, _projection, sink);
        await isolated.Handle(Incoming(_first, "isolated", 1));
        ClearSharedCalls();
    }

    async Task Because()
    {
        _live = await _manager.GetFor(_store, _namespace, _projection);
        await _live.BeginReplay(_replay);
        await _live.Handle(Incoming(_first, "live-first", 2));
        await _live.Handle(Incoming(_second, "live-second", 3));
        await _live.EndReplay(_replay);
        _cached = await _manager.GetFor(_store, _namespace, _projection);
    }

    [Fact] void should_cache_the_live_pipeline() => _cached.ShouldBeSame(_live);
    [Fact] void should_resolve_futures_under_the_projection_id_once() => _grainFactory.Received(1).GetGrain<IProjectionFutures>(new ProjectionFuturesKey(_projection.Identifier, _store, _namespace).ToString(), null);
    [Fact] async Task should_select_the_live_sink_once() => await _namespaceStorage.Sinks.Received(1).GetFor(_readModel);
    [Fact] async Task should_begin_shared_changeset_replay() => await _sharedChangesets.Received(1).BeginReplay(_readModel.ContainerName);
    [Fact] async Task should_end_shared_changeset_replay() => await _sharedChangesets.Received(1).EndReplay(_readModel.ContainerName);
    [Fact] async Task should_notify_the_live_sink_of_replay() => await _liveSink.Received(1).BeginReplay(_replay);
    [Fact] async Task should_notify_the_live_sink_when_replay_ends() => await _liveSink.Received(1).EndReplay(_replay);
    [Fact] async Task should_store_both_live_changesets() => await _sharedChangesets.Received(2).Save(_readModel.ContainerName, Arg.Any<Key>(), Arg.Any<EventType>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CorrelationId>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>());
    [Fact] void should_write_the_first_live_value() => ((IDictionary<string, object?>)_saved.Single().Value)["oldName"].ShouldEqual("live-first");
    [Fact] void should_write_the_second_live_value() => ((IDictionary<string, object?>)_saved.Single().Value)["newName"].ShouldEqual("live-second");
}
