// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing;

public class and_observer_has_been_put_into_rewind_state : given.an_observer_and_two_event_types
{
    async Task Establish()
    {
        state.NextEventSequenceNumber = 42;
        state.RunningState = ObserverRunningState.Disconnected;
        await observer.Rewind();
    }

    async Task Because() => await observer.Subscribe<ObserverSubscriber>(event_types, subscriber_args);

    [Fact] void should_set_state_to_replaying() => state.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_initiate_replay() => replay.Verify(_ => _.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args)), Once);
}
