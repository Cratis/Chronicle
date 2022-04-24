// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Observation;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_subscribing;

public class and_catches_up_to_tail : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    async Task Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_types, null)).Returns(Task.FromResult(EventSequenceNumber.First));
        await observer.Subscribe(event_types, observer_namespace);
    }

    async Task Because() => await observers[0].OnNextAsync(appended_event);

    [Fact] void should_have_running_state_of_active() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Active);
}
