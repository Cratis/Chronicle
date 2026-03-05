// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.when_activating;

public class with_unknown_current_state : given.a_state_machine
{
    Exception _exception;

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() => [new StateThatSupportsTransitioningFrom()];

    void Establish()
    {
        // We get the state machine so that it calls the OnActivateAsync method before we set the state to test for
        _ = StateMachine;
        _stateStorage.State = new StateMachineStateForTesting { CurrentState = "Something" };
    }

    async Task Because() => _exception = await Catch.Exception(async () => await StateMachine.OnActivateAsync(CancellationToken.None));

    [Fact] void should_throw_invalid_type_for_state_exception() => _exception.ShouldBeOfExactType<UnknownCurrentState>();
}
