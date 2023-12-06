// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_transitioning;

public class and_state_transitions_to_other_state_during_on_enter : given.a_state_machine_with_well_known_states
{
    protected override Type initial_state => typeof(StateThatSupportsTransitioningFrom);

    async Task Because() => await state_machine.TransitionTo<StateThatTransitionsOnEnter>();

    [Fact] void should_finish_state_before_transitioning() => (on_calls[2].Type == typeof(StateThatTransitionsOnEnter) && on_calls[2].IsEnter).ShouldBeTrue();
    [Fact] void should_leave_state_before_transitioning() => (on_calls[3].Type == typeof(StateThatTransitionsOnEnter) && !on_calls[3].IsEnter).ShouldBeTrue();
    [Fact] void should_transition_to_target_after_first_transition_is_done() => (on_calls[4].Type == typeof(StateThatTransitionsOnLeave) && on_calls[4].IsEnter).ShouldBeTrue();
}
