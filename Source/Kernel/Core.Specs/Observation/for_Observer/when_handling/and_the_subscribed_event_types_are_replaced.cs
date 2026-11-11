// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_subscribed_event_types_are_replaced : given.an_observer_with_subscription_for_specific_event_type
{
    readonly EventType _replacement = new("Replacement", EventTypeGeneration.First);

    void Establish() =>
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(44UL));

    async Task Because()
    {
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        _subscriber.ClearReceivedCalls();
        _observer.SetSubscription(subscription with { EventTypes = [_replacement] });
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 45UL)]);
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_replacement, 46UL)]);
    }

    [Fact] void should_only_deliver_the_replacement_type_after_replacing_the_subscription() =>
        _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.All(@event => @event.Context.EventType.Id == _replacement.Id)), Arg.Any<ObserverSubscriberContext>());

    [Fact] void should_not_deliver_the_previous_type_after_replacing_the_subscription() =>
        _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Any(@event => @event.Context.EventType.Id == event_type.Id)), Arg.Any<ObserverSubscriberContext>());
}
