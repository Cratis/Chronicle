// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_rewinding;

public class and_is_already_in_replay : given.an_observer_and_two_event_types
{
    async Task Establish()
    {
        await observer.Rewind();
    }

    async Task Because() => await observer.Rewind();

    [Fact] void should_unsubscribe_subscription() => subscription_handles[0].Verify(_ => _.UnsubscribeAsync(), Once());
    [Fact] void should_set_running_state_to_replaying() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_subscribe_from_first_event_sequence() => subscribed_tokens[1].SequenceNumber.ShouldEqual((long)EventSequenceNumber.First.Value);
}
