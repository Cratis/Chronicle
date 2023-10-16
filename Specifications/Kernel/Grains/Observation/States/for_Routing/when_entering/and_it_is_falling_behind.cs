// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Routing.when_entering;

public class and_it_is_falling_behind : given.a_routing_state
{
    void Establish() =>
        tail_event_sequence_numbers = tail_event_sequence_numbers with
        {
            Tail = 42,
            TailForEventTypes = 22
        };

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => state_machine.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_catch_up() => state_machine.Verify(_ => _.TransitionTo<CatchUp>(), Once());
    [Fact] void should_set_next_event_sequence_number_to_next_after_tail() => resulting_stored_state.NextEventSequenceNumber.ShouldEqual(tail_event_sequence_numbers.Tail.Next());
}
