// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_creating_an_isolated_pipeline;

public class and_materializing_events : given.a_materializing_pipeline_manager
{
    PartitionRebuildSink _firstSink;
    PartitionRebuildSink _secondSink;
    IProjectionPipeline _live;
    IProjectionPipeline _liveAfter;

    async Task Establish()
    {
        _live = await _manager.GetFor(_store, _namespace, _projection);
        _firstSink = new(new Key("source", ArrayIndexers.NoIndexers));
        _secondSink = new(new Key("source", ArrayIndexers.NoIndexers));
        ClearSharedCalls();
    }

    async Task Because()
    {
        var first = _manager.CreateIsolated(_store, _namespace, _projection, _firstSink);
        var second = _manager.CreateIsolated(_store, _namespace, _projection, _secondSink);
        await first.BeginReplay(_replay);
        await second.BeginReplay(_replay);
        await first.Handle(Incoming(_first, "first", 1));
        await first.Handle(Incoming(_second, "second", 2));
        await second.Handle(Incoming(_first, "other", 3));
        await first.EndReplay(_replay);
        await second.EndReplay(_replay);
        _liveAfter = await _manager.GetFor(_store, _namespace, _projection);
    }

    [Fact] void should_apply_the_first_event() => ((IDictionary<string, object?>)_firstSink.State!)["oldName"].ShouldEqual("first");
    [Fact] void should_apply_the_second_event() => ((IDictionary<string, object?>)_firstSink.State!)["newName"].ShouldEqual("second");
    [Fact] void should_track_the_first_pipelines_watermark() => _firstSink.Watermark.ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_keep_the_second_pipelines_state_separate() => ((IDictionary<string, object?>)_secondSink.State!)["oldName"].ShouldEqual("other");
    [Fact] void should_track_the_second_pipelines_watermark() => _secondSink.Watermark.ShouldEqual((EventSequenceNumber)3);
    [Fact] void should_not_call_shared_changeset_storage() => _sharedChangesets.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_shared_futures() => _sharedFutures.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_a_futures_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_touch_the_live_sink() => _liveSink.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_the_cached_live_pipeline() => _liveAfter.ShouldBeSame(_live);
}
