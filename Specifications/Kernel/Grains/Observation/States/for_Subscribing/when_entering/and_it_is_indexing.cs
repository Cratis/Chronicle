// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.States.for_Subscribing.when_entering;

public class and_it_is_indexing : given.a_subscribing_state
{
    void Establish() => stored_state.RunningState = ObserverRunningState.Indexing;

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_only_perform_one_transition() => state_machine.Verify(_ => _.TransitionTo<IState<ObserverState>>(), Once());
    [Fact] void should_transition_to_indexing() => state_machine.Verify(_ => _.TransitionTo<Indexing>(), Once());
    [Fact] void should_set_next_event_sequence_number_to_next_after_tail() => resulting_stored_state.NextEventSequenceNumber.ShouldEqual(tail_event_sequence_numbers.Tail.Next());
}
