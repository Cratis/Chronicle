// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.StateMachines.when_transitioning;

public class and_state_does_not_change : given.a_state_machine
{
    protected override Type InitialState => typeof(StateThatDoesNotChangeStateOnSelfTransition);

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() => [new StateThatDoesNotChangeStateOnSelfTransition()];

    void Establish()
    {
        // Force the lazy grain creation so the initial-state transition and its write are done before we measure.
        _ = StateMachine;
        _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().ResetCounts();
    }

    async Task Because() => await StateMachine.TransitionTo<StateThatDoesNotChangeStateOnSelfTransition>();

    [Fact] void should_not_write_state_when_nothing_changed() => _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().Writes.ShouldEqual(0);
    [Fact] async Task should_remain_in_the_same_state() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<StateThatDoesNotChangeStateOnSelfTransition>();
}
