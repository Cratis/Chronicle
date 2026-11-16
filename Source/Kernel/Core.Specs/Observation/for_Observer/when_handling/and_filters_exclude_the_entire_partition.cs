// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_filters_exclude_the_entire_partition : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        subscription = subscription with { Filters = new ObserverFilters([], new EventSourceType("wanted")) };
        _observer.SetSubscription(subscription);
    }

    async Task Because() => await _observer.Handle("source", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_not_invoke_the_subscriber() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_advance_the_next_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_leave_in_flight_state() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
}
