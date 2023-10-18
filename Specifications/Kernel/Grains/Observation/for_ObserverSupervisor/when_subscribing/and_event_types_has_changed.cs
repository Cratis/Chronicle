// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing;

public class and_event_types_has_changed : given.an_observer_and_two_event_types
{
    protected IEnumerable<EventType> new_event_types = new EventType[]
    {
        new("ad9f43ca-8d98-4669-99cd-dbd0eaaea9d9", 1),
        new("3e84ef60-c725-4b45-832d-29e3b663d7cd", 1),
        new("779b7d07-7a78-4c9c-a925-1c68d035cf1b", 1)
    };

    void Establish()
    {
        state.RunningState = ObserverRunningState.Active;
        state.NextEventSequenceNumber = 1;
        event_sequence.Setup(_ => _.GetTailSequenceNumberForEventTypes(event_types)).Returns(Task.FromResult((EventSequenceNumber)0));
    }

    async Task Because() => await observer.Subscribe<ObserverSubscriber>(new_event_types, subscriber_args);

    [Fact] void should_set_state_to_replaying() => state.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_initiate_replay() => replay.Verify(_ => _.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), new_event_types, typeof(ObserverSubscriber), subscriber_args)), Once);
}
