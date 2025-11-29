// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.StateMachines.when_transitioning;

public class and_state_is_unknown_state_type : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatSupportsTransitioningFrom);
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(StateMachine.TransitionTo<UnknownState>);

    [Fact] void should_throw_an_unknown_state_type_in_state_machine_exception() => _exception.ShouldBeOfExactType<UnknownStateTypeInStateMachine>();
}
