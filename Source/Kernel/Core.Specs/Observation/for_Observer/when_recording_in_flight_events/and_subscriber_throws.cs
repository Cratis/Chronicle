// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_in_flight_events;

public class and_subscriber_throws : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish() =>
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Throws(new InvalidOperationException("boom"));

    async Task Because() =>
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_still_record_the_event_as_in_flight_before_dispatch() => _inFlightEventsStorage
        .Received(1)
        .Add(Arg.Any<ObserverId>(), Arg.Is<Key>(_ => _.Value == "Something"), Arg.Is<EventSequenceNumber>(_ => _ == 42UL));
}
