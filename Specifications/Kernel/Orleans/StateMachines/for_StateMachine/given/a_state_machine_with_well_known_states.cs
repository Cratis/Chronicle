// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.given;

public abstract class a_state_machine_with_well_known_states : a_state_machine
{
    protected StateThatSupportsTransitioning state_that_supports_transitioning;
    protected StateThatDoesNotSupportTransitioning state_that_does_not_support_transitioning;
    protected StateThatTransitionsOnEnter state_that_transitions_on_enter;
    protected StateThatTransitionsOnLeave state_that_transitions_on_leave;
    protected List<(Type Type, bool IsEnter, StateMachineState State)> on_calls;

    void Establish()
    {
        on_calls = new();
        state_that_supports_transitioning.OnEnterCalled = _ => AddCall(typeof(StateThatSupportsTransitioning), true, _);
        state_that_supports_transitioning.OnLeaveCalled = _ => AddCall(typeof(StateThatSupportsTransitioning), false, _);
        state_that_does_not_support_transitioning.OnEnterCalled = _ => AddCall(typeof(StateThatDoesNotSupportTransitioning), true, _);
        state_that_does_not_support_transitioning.OnLeaveCalled = _ => AddCall(typeof(StateThatDoesNotSupportTransitioning), false, _);
        state_that_transitions_on_enter.OnEnterCalled = _ => AddCall(typeof(StateThatTransitionsOnEnter), true, _);
        state_that_transitions_on_enter.OnLeaveCalled = _ => AddCall(typeof(StateThatTransitionsOnEnter), false, _);
        state_that_transitions_on_leave.OnEnterCalled = _ => AddCall(typeof(StateThatTransitionsOnLeave), true, _);
        state_that_transitions_on_leave.OnLeaveCalled = _ => AddCall(typeof(StateThatTransitionsOnLeave), false, _);
        state_that_does_not_support_transitioning.StateToReturnOnLeave = new() { Something = "Leave State" };
        state_that_supports_transitioning.StateToReturnOnEnter = new() { Something = "Enter State" };
    }

    void AddCall(Type type, bool isEnter, StateMachineState state) => on_calls.Add((type, isEnter, state));

    protected override IEnumerable<IState<StateMachineState>> CreateStates()
    {
        state_that_supports_transitioning = new();
        state_that_does_not_support_transitioning = new();
        state_that_transitions_on_enter = new();
        state_that_transitions_on_leave = new();

        return new IState<StateMachineState>[]
        {
            state_that_supports_transitioning,
            state_that_does_not_support_transitioning,
            state_that_transitions_on_enter,
            state_that_transitions_on_leave
        };
    }
}
