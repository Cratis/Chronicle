// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.given;

public abstract class a_state_machine_with_two_well_known_states : a_state_machine
{
    protected StateThatSupportsTransitioning state_that_supports_transitioning;
    protected StateThatDoesNotSupportTransitioning state_that_does_not_support_transitioning;
    protected List<(Type Type, bool IsEnter, StateMachineState State)> on_calls;

    void Establish()
    {
        on_calls = new();
        state_that_does_not_support_transitioning.OnEnterCalled = _ => on_calls.Add((typeof(StateThatDoesNotSupportTransitioning), true, _));
        state_that_supports_transitioning.OnEnterCalled = _ => on_calls.Add((typeof(StateThatSupportsTransitioning), true, _));
        state_that_does_not_support_transitioning.OnLeaveCalled = _ => on_calls.Add((typeof(StateThatDoesNotSupportTransitioning), false, _));
        state_that_supports_transitioning.OnLeaveCalled = _ => on_calls.Add((typeof(StateThatSupportsTransitioning), false, _));
        state_that_does_not_support_transitioning.StateToReturnOnLeave = new() { Something = "Leave State" };
        state_that_supports_transitioning.StateToReturnOnEnter = new() { Something = "Enter State" };
    }

    protected override IEnumerable<IState<StateMachineState>> CreateStates()
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
