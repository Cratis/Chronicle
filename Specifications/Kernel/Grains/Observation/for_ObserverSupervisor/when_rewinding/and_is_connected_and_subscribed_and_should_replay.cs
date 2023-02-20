// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_rewinding;

public class and_is_connected_and_subscribed_and_should_replay : given.an_observer_and_two_event_types
{
    async Task Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).ReturnsAsync(new EventSequenceNumber(42L));
        state.RunningState = ObserverRunningState.Active;
        state.NextEventSequenceNumber = EventSequenceNumber.First;
        await observer.Subscribe<ObserverSubscriber>(event_types, subscriber_args);
    }

    async Task Because() => await observer.Rewind();

    [Fact] void should_set_running_state_to_replay() => state.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_set_next_event_sequence_number_to_first() => state.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_initiate_replay() => replay.Verify(_ => _.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args)), Once);
}
