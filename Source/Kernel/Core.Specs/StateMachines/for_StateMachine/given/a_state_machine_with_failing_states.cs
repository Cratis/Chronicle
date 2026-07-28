// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.given;

/// <summary>
/// A state machine that also knows a state failing on enter and one failing on leave, so a spec can drive a
/// transition into a failure and then check the machine still works.
/// </summary>
public abstract class a_state_machine_with_failing_states : a_state_machine
{
    protected StateThatSupportsTransitioningFrom state_that_supports_transitioning;
    protected StateThatDoesNotChangeStateOnSelfTransition state_to_recover_into;
    protected StateThrowingOnEnter state_throwing_on_enter;
    protected StateThrowingOnLeave state_throwing_on_leave;

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates()
    {
        state_that_supports_transitioning = new();
        state_to_recover_into = new();
        state_throwing_on_enter = new();
        state_throwing_on_leave = new();

        return
        [
            state_that_supports_transitioning,
            state_to_recover_into,
            state_throwing_on_enter,
            state_throwing_on_leave
        ];
    }

    protected async Task<Exception?> TransitionTo<TState>()
        where TState : IState<StateMachineStateForTesting>
    {
        try
        {
            await StateMachine.TransitionTo<TState>();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
