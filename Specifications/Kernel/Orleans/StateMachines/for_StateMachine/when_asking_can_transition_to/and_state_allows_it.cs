// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_asking_can_transition_to;

public class and_state_allows_it : given.a_state_machine_with_well_known_states
{
    bool result;

    protected override Type initial_state => typeof(StateThatSupportsTransitioningFrom);

    async Task Because() => result = await state_machine.CanTransitionTo<StateThatDoesNotSupportTransitioningFrom>();

    [Fact] void should_be_able_to_transition() => result.ShouldBeTrue();
}
