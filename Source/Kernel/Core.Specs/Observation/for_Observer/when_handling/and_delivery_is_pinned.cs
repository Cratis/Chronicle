// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_delivery_is_pinned : given.an_observer_with_subscription_and_schema_for_event_type
{
    AppendedEvent _selected;
    AppendedEvent[] _delivered = [];

    async Task Establish()
    {
        _eventTypesStorage.HasFor(event_type.Id, event_type.Generation).Returns(true);
        await _observer.SubscribeWithGenerationDelivery<IObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, EventGenerationDelivery.Pinned);
        _selected = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL);
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns([_selected]);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(call =>
        {
            _delivered = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
            return ObserverSubscriberResult.Ok(42UL);
        });
    }

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_deliver_the_kernel_selected_event() => _delivered.ShouldContainOnly(_selected);
    [Fact] async Task should_not_use_compatibility_release() => await _eventCompliance.DidNotReceive().Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>());
}
