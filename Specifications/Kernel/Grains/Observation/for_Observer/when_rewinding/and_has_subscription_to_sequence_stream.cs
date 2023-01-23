// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Observer.when_rewinding;

public class and_has_subscription_to_sequence_stream : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    async Task Establish()
    {
        await observer.Subscribe<ObserverSubscriber>(event_types);

        state.RunningState = ObserverRunningState.Active;
        state.NextEventSequenceNumber = 1;
    }

    async Task Because() => await observer.Rewind();

    [Fact] void should_unsubscribe_subscription() => subscription_handles[0].Verify(_ => _.UnsubscribeAsync(), Once());
    [Fact] void should_set_running_state_to_replaying() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_subscribe_from_first_event_sequence() => subscribed_tokens[1].SequenceNumber.ShouldEqual((long)EventSequenceNumber.First.Value);
}
