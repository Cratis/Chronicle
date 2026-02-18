// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_partition_is_replaying : given.an_observer_with_subscription_for_specific_event_type
{
    const string EventSourceId = "Something";

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 53UL,
            LastHandledEventSequenceNumber = 54UL
        };
        _stateStorage.State.ReplayingPartitions.Add(EventSourceId);
    }

    async Task Because() => await _observer.Handle(EventSourceId, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);

    [Fact] void should_not_forward_to_subscriber() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_set_next_sequence_number() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)53UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
