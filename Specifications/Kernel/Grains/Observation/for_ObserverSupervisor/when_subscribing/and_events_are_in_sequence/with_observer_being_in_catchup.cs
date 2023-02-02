// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_subscribing.and_events_are_in_sequence;

public class with_observer_being_in_catchup : given.an_observer_and_two_event_types_and_one_event_in_sequence
{
    void Establish()
    {
        state.RunningState = ObserverRunningState.CatchingUp;
    }

    async Task Because()
    {
        await observer.Subscribe<ObserverSubscriber>(event_types);
        await observers[0].OnNextAsync(appended_event);
    }

    [Fact] void should_forward_event_to_observer_subscriber() => subscriber.Verify(_ => _.OnNext(appended_event), Once());
    [Fact] void should_set_offset_to_next_event_sequence() => state_on_write.NextEventSequenceNumber.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value + 1);
    [Fact] void should_set_last_handled_to_event_sequence_number_from_event() => state_on_write.LastHandled.Value.ShouldEqual(appended_event.Metadata.SequenceNumber.Value);
    [Fact] void should_set_running_state_to_active() => state_on_write.RunningState.ShouldEqual(ObserverRunningState.Active);
}
