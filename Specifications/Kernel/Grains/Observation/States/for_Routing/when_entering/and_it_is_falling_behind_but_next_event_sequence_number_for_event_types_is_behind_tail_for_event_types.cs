// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Kernel.Storage.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing.when_entering;

public class and_it_is_falling_behind_but_next_event_sequence_number_for_event_types_is_behind_tail_for_event_types : given.a_routing_state
{
    void Establish()
    {
        stored_state = stored_state with
        {
            NextEventSequenceNumberForEventTypes = 22
        };

        tail_event_sequence_numbers = tail_event_sequence_numbers with
        {
            Tail = 42,
            TailForEventTypes = 21
        };
    }

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => observer.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_catch_up() => observer.Verify(_ => _.TransitionTo<Observing>(), Once());
    [Fact] void should_update_next_event_sequence_number_to_tail() => resulting_stored_state.NextEventSequenceNumber.Value.ShouldEqual(43UL);
}
