// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_handling;

public class and_subsciption_is_disconnected : given.an_observer
{
    void Establish()
    {
        state.NextEventSequenceNumber = 53UL;
        state.LastHandled = 54UL;
    }

    async Task Because() => await observer.Handle("Something", new[] { AppendedEvent.EmptyWithEventSequenceNumber(43UL) });

    [Fact] void should_not_forward_to_subscriber() => subscriber.Verify(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()), Never);
    [Fact] void should_not_set_next_sequence_number() => state.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)53UL);
    [Fact] void should_not_set_last_handled_event_sequence_number() => state.LastHandled.ShouldEqual((EventSequenceNumber)54UL);
    [Fact] void should_not_write_state() => written_states.Count.ShouldEqual(0);
}


public class and_events_has_already_been_handled : given.an_observer_with_subscription
{
}

public class and_some_events_has_already_been_handled : given.an_observer_with_subscription
{
}
