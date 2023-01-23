// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_subscribing.and_events_are_in_sequence;

public class and_observer_is_ahead : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        state.NextEventSequenceNumber = 42U;
        state.LastHandled = 41U;
    }

    async Task Because()
    {
        await observer.Subscribe<ObserverSubscriber>(event_types);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_not_forward_event_to_observer_subscriber() => subscriber.Verify(_ => _.OnNext(appended_event), Never());
    [Fact] void should_not_change_offset() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(42U);
    [Fact] void should_not_change_set_last_handled() => state_on_write.LastHandled.Value.ShouldEqual(41U);
}
