// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_in_flight_events;

public class and_subscriber_is_successful : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish() =>
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(42UL));

    async Task Because() =>
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_record_the_event_as_in_flight_before_dispatch() => _inFlightEventsStorage
        .Received(1)
        .Add(Arg.Any<ObserverId>(), Arg.Is<Key>(_ => (string)_.Value == "Something"), Arg.Is<EventSequenceNumber>(_ => _ == 42UL));

    [Fact] void should_clear_the_in_flight_entry_after_success() => _inFlightEventsStorage
        .Received(1)
        .RemoveUpTo(Arg.Any<ObserverId>(), Arg.Is<Key>(_ => (string)_.Value == "Something"), Arg.Is<EventSequenceNumber>(_ => _ == 42UL));
}
