// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_in_flight_events;

public class and_subscriber_is_successful : given.an_observer_with_subscription_for_specific_event_type
{
    ISet<Key> _inFlightPartitionsDuringDispatch;

    void Establish() =>
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ =>
            {
                // Capture what has been made durable at the moment the subscriber is invoked - the in-flight marker
                // must already be persisted on the observer state before dispatch.
                _inFlightPartitionsDuringDispatch = new HashSet<Key>(_stateStorage.State.InFlightPartitions);
                return ObserverSubscriberResult.Ok(42UL);
            });

    async Task Because() =>
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_record_the_partition_as_in_flight_on_the_state_before_dispatch() =>
        _inFlightPartitionsDuringDispatch.ShouldContain((Key)"Something");

    [Fact] void should_clear_the_in_flight_partition_from_the_state_after_success() =>
        _stateStorage.State.InFlightPartitions.ShouldNotContain((Key)"Something");

    [Fact] void should_not_record_the_marker_through_a_separate_in_flight_store() => _inFlightEventsStorage
        .DidNotReceive()
        .Add(Arg.Any<ObserverId>(), Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());

    [Fact] void should_not_clear_the_marker_through_a_separate_in_flight_store() => _inFlightEventsStorage
        .DidNotReceive()
        .RemoveUpTo(Arg.Any<ObserverId>(), Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}
