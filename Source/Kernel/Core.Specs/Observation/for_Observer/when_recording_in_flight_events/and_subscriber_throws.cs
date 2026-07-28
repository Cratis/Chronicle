// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_in_flight_events;

public class and_subscriber_throws : given.an_observer_with_subscription_for_specific_event_type
{
    ISet<Key> _inFlightPartitionsDuringDispatch;

    void Establish() =>
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns<ObserverSubscriberResult>(_ =>
            {
                // The marker must be durable before dispatch even when the subscriber then fails mid-handling,
                // so a crash at this point recovers the partition.
                _inFlightPartitionsDuringDispatch = new HashSet<Key>(_stateStorage.State.InFlightPartitions);
                throw new InvalidOperationException("boom");
            });

    async Task Because() =>
        await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_still_record_the_partition_as_in_flight_on_the_state_before_dispatch() =>
        _inFlightPartitionsDuringDispatch.ShouldContain((Key)"Something");

    [Fact] void should_not_record_the_marker_through_a_separate_in_flight_store() => _inFlightEventsStorage
        .DidNotReceive()
        .Add(Arg.Any<ObserverId>(), Arg.Any<Key>(), Arg.Any<EventSequenceNumber>());
}
