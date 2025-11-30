// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.StateMachines.when_activating;

public class with_valid_initial_state_type : given.a_state_machine
{
    Exception _exception;

    protected override Type InitialState => typeof(StateThatSupportsTransitioningFrom);
    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() => [new StateThatSupportsTransitioningFrom()];

    async Task Because() => _exception = await Catch.Exception(async () => await StateMachine.OnActivateAsync(CancellationToken.None));

    [Fact] void should_not_throw_invalid_type_for_state_exception() => _exception.ShouldBeNull();
}
