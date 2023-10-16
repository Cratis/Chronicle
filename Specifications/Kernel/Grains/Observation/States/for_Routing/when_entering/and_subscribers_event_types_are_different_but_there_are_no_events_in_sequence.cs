// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing.when_entering;

public class and_subscribers_event_types_are_different_but_there_are_no_events_in_sequence : given.a_routing_state
{
    void Establish()
    {
        stored_state.EventTypes = new[]
        {
            new EventType("31252720-dcbb-47ae-927d-26070f7ef8ae", EventGeneration.First)
        };
        subscription = subscription with
        {
            EventTypes = new[]
            {
                new EventType("e433be87-2d05-49b1-b093-f0cec977429b", EventGeneration.First)
            }
        };

        tail_event_sequence_numbers = tail_event_sequence_numbers with
        {
            Tail = EventSequenceNumber.Unavailable,
            TailForEventTypes = EventSequenceNumber.Unavailable
        };
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => state_machine.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_observing() => state_machine.Verify(_ => _.TransitionTo<Observing>(), Once());
    [Fact] void should_set_next_event_sequence_number_to_first() => resulting_stored_state.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_set_event_types_to_subscribers_event_types() => resulting_stored_state.EventTypes.ShouldEqual(subscription.EventTypes);
}
