// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_the_pipeline_is_replaced_after_a_window_opened_mid_delivery : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _first;
    AppendedEvent _second;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _first = AppendedEvent.EmptyWithEventType(new("first-event", 1));
        _first = _first with { Context = _first.Context with { SequenceNumber = 42 } };
        _second = AppendedEvent.EmptyWithEventType(new("second-event", 1));
        _second = _second with { Context = _second.Context with { SequenceNumber = 43 } };

        // Pipeline A opens a buffering window while it handles the first event, then the second event forces a refresh to pipeline B.
        _projection.Accepts(_second.Context.EventType).Returns(false);
        _cachedPipeline.BufferingWindow.Returns(new SinkBufferingWindow(2), new SinkBufferingWindow(3));
        _cachedPipeline.Handle(_first).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _first));
        var updated = _definition with { IsRewindable = false };
        _storage.GetEventStore(_key.EventStore).Projections.Get(_key.ObserverId).Returns(updated);
        _factory.Create(_key.EventStore, _key.Namespace, updated, Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>())
            .Returns(_ =>
            {
                _projection.Accepts(_second.Context.EventType).Returns(true);
                return _projection;
            });
        _refreshedPipeline.BufferingWindow.Returns(new SinkBufferingWindow(0));
        _refreshedPipeline.Handle(_second).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _second));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_first, _second], new(null) { TrackPersistence = true });

    [Fact] void should_handle_the_second_event_with_the_refreshed_pipeline() => _ = _refreshedPipeline.Received(1).Handle(_second);
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_report_the_writes_as_persisted() => _result.WritesPersisted.ShouldBeFalse();
}
