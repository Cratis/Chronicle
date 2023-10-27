// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_event_is_of_a_type_the_observer_is_not_interested_in : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        state.NextEventSequenceNumber = 53UL;
        state.LastHandledEventSequenceNumber = 52UL;
    }

    async Task Because() => await observer.Handle("Something", new[] { AppendedEvent.EmptyWithEventSequenceNumber(53UL) });

    [Fact] void should_not_forward_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Never);
    [Fact] void should_set_next_sequence_number() => state.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_write_state_once() => written_states.Count.ShouldEqual(1);
}
