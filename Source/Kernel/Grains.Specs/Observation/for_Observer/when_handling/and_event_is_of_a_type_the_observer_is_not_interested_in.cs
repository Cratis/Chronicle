// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.when_handling;

public class and_event_is_of_a_type_the_observer_is_not_interested_in : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        state_storage.State = state_storage.State with
        {
            NextEventSequenceNumber = 53UL,
            LastHandledEventSequenceNumber = 52UL
        };
    }

    async Task Because() => await observer.Handle("Something", [AppendedEvent.EmptyWithEventSequenceNumber(53UL)]);

    [Fact] void should_not_forward_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Never);
    [Fact] void should_set_next_sequence_number() => state_storage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_write_state_once() => storage_stats.Writes.ShouldEqual(1);
}
