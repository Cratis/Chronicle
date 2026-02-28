// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.when_transitioning;

public class and_state_transitions_to_other_state_during_on_leave : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatTransitionsOnLeave);
    Exception _result;

    async Task Because() => _result = await Catch.Exception(StateMachine.TransitionTo<StateThatSupportsTransitioningFrom>);

    [Fact] void should_throw_transitioning_during_on_leave_is_not_supported() => _result.ShouldBeOfExactType<TransitioningDuringOnLeaveIsNotSupported>();
}
