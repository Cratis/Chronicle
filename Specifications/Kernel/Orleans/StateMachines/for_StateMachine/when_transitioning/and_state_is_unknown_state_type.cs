// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_transitioning;

public class and_state_is_unknown_state_type : given.a_state_machine_with_well_known_states
{
    protected override Type initial_state => typeof(StateThatSupportsTransitioningFrom);
    Exception exception;

    async Task Because() => exception = await Catch.Exception(state_machine.TransitionTo<UnknownState>);

    [Fact] void should_throw_an_unknown_state_type_in_state_machine_exception() => exception.ShouldBeOfExactType<UnknownStateTypeInStateMachine>();
}
