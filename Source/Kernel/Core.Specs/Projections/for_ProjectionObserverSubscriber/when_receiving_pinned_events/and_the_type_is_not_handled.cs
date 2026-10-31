// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_pinned_events;

public class and_the_type_is_not_handled : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _event = _event with { Context = _event.Context with { EventType = new("the-dropped-event", 1) } };

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_not_hand_the_event_to_the_pipeline() => _ = _cachedPipeline.DidNotReceive().Handle(Arg.Any<AppendedEvent>());
    [Fact] void should_not_release_the_event() => _ = _projection.DidNotReceive().ReleaseEvent(Arg.Any<EventType>(), Arg.Any<AppendedEvent>());
    [Fact] void should_not_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_move_the_observer_past_the_event() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
}
