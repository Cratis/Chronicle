// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_unsubscribing;

public class and_observer_is_subscribed : given.an_observer_and_two_event_types
{
    async Task Establish()
    {
        event_sequence.Setup(_ => _.GetNextSequenceNumber()).Returns(Task.FromResult((EventSequenceNumber)1));
        await observer.Subscribe(event_types, observer_namespace);
    }

    async Task Because() => await observer.Unsubscribe();

    [Fact] void should_set_current_namespace_to_not_set() => state_on_write.CurrentNamespace.ShouldEqual(ObserverNamespace.NotSet);
    [Fact] void should_set_state_to_active() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] void should_unsubscribe_subscription() => subscription_handles[0].Verify(_ => _.UnsubscribeAsync(), Once());
}
