// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.given;

public abstract class a_state_machine_with_two_well_known_states : a_state_machine
{
    protected StateThatSupportsTransitioning state_that_supports_transitioning;
    protected StateThatDoesNotSupportTransitioning state_that_does_not_support_transitioning;

    protected override IEnumerable<IState<StateMachineState>> GetStates()
    {
        state_that_supports_transitioning = new();
        state_that_does_not_support_transitioning = new();

        return new IState<StateMachineState>[]
        {
            state_that_supports_transitioning,
            state_that_does_not_support_transitioning
        };
    }
}
