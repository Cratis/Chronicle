// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_asking_can_transition_to;

public class and_state_does_not_allow_it : given.a_state_machine
{
    bool result;

    protected override IEnumerable<IState<StateMachineState>> GetStates() => new IState<StateMachineState>[]
    {
        new StateThatSupportsTransitioning(),
        new StateThatDoesNotSupportTransitioning()
    };

    async Task Because() => result = await state_machine.CanTransitionTo<StateThatDoesNotSupportTransitioning>();

    [Fact] void should_not_be_able_to_transition() => result.ShouldBeFalse();
}
