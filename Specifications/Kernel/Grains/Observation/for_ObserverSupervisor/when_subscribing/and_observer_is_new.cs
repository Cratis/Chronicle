// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing;

public class and_observer_is_new : given.an_observer_and_two_event_types
{
    void Establish()
    {
        state.RunningState = ObserverRunningState.New;
        state.NextEventSequenceNumber = 0;
        state.EventTypes = Array.Empty<EventType>();
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)1));
    }

    async Task Because() => await observer.Subscribe<ObserverSubscriber>(event_types, subscriber_args);

    [Fact] void should_set_state_to_catching_up() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.CatchingUp);
    [Fact] void should_initiate_catchup() => catch_up.Verify(_ => _.Start(typeof(ObserverSubscriber), subscriber_args), Once);
}
