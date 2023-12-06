// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_transitioning;

public class and_state_transitions_to_other_state_during_on_leave : given.a_state_machine_with_well_known_states
{
    protected override Type initial_state => typeof(StateThatTransitionsOnLeave);
    Exception result;

    async Task Because() => result = await Catch.Exception(state_machine.TransitionTo<StateThatSupportsTransitioningFrom>);

    [Fact] void should_throw_transitioning_during_on_leave_is_not_supported() => result.ShouldBeOfExactType<TransitioningDuringOnLeaveIsNotSupported>();
}
