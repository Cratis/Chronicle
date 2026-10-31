// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_a_buffering_window_is_open : given.a_subscriber_with_a_cached_pipeline
{
    AppendedEvent _event;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new("an-event", 1));
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _cachedPipeline.Handle(_event).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _event));
        _cachedPipeline.BufferingWindow.Returns(new SinkBufferingWindow(3));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null) { TrackPersistence = true });

    [Fact] void should_be_ok() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_report_the_writes_as_persisted() => _result.WritesPersisted.ShouldBeFalse();
}
