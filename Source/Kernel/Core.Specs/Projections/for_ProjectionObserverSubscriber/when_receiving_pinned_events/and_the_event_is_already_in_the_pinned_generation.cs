// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_pinned_events;

public class and_the_event_is_already_in_the_pinned_generation : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _event = _event with { Context = _event.Context with { EventType = _pin } };

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_process_the_event_once() => _ = _cachedPipeline.Received(1).Handle(_event);
    [Fact] void should_not_release_plaintext_again() => _ = _projection.DidNotReceive().ReleaseEvent(Arg.Any<EventType>(), Arg.Any<AppendedEvent>());
    [Fact] void should_handle_the_delivery_successfully() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
