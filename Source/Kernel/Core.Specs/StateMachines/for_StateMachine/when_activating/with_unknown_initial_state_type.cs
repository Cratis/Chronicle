// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.when_activating;

public class with_unknown_initial_state_type : given.a_state_machine
{
    Exception _exception;

    protected override Type InitialState => typeof(StateThatDoesNotSupportTransitioningFrom);

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new StateThatSupportsTransitioningFrom()
    ];

    void Because() => _exception = Catch.Exception(() => _ = StateMachine);

    [Fact] void should_throw_invalid_type_for_state_exception() => _exception.ShouldBeOfExactType<UnknownStateTypeInStateMachine>();
}
