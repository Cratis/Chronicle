// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_partition_is_replaying : given.an_observer_with_subscription_for_specific_event_type
{
    const string event_source_id = "Something";

    void Establish()
    {
        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = 53UL,
            LastHandledEventSequenceNumber = 54UL
        };
        state_storage.State.ReplayingPartitions.Add(event_source_id);
    }

    async Task Because() => await observer.Handle(event_source_id, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);

    [Fact] void should_not_forward_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Never);
    [Fact] void should_not_set_next_sequence_number() => state_storage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)53UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state_storage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_not_write_state() => storage_stats.Writes.ShouldEqual(0);
}
