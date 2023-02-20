// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_rewinding;

public class and_is_disconnected : given.an_observer_and_two_event_types
{
    void Establish()
    {
        state.RunningState = ObserverRunningState.Disconnected;
        state.NextEventSequenceNumber = EventSequenceNumber.First;
    }

    async Task Because() => await observer.Rewind();

    [Fact] void should_set_running_state_to_rewinding() => state.RunningState.ShouldEqual(ObserverRunningState.Rewinding);
    [Fact] void should_set_next_event_sequence_number_to_first() => state.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_initiate_replay() => replay.Verify(_ => _.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args)), Never);
    [Fact] void should_write_state() => persistent_state.Verify(_ => _.WriteStateAsync(), Once);
}
