// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.when_transitioning;

/// <summary>
/// Leaving a state unsubscribes and writes storage, so it can fail too. If the leaving flag were left raised, every
/// later transition would be rejected with TransitioningDuringOnLeaveIsNotSupported - an exception thrown out of
/// whatever tried to recover the machine, blaming a transition that is not in progress.
/// </summary>
public class and_on_leave_throws : given.a_state_machine_with_failing_states
{
    protected override Type InitialState => typeof(StateThrowingOnLeave);

    Exception? _failure;
    Exception? _laterFailure;

    async Task Because()
    {
        _failure = await TransitionTo<StateThatDoesNotChangeStateOnSelfTransition>();
        state_throwing_on_leave.ShouldThrow = false;
        _laterFailure = await TransitionTo<StateThatDoesNotChangeStateOnSelfTransition>();
    }

    [Fact] void should_surface_the_failure_to_the_caller() => _failure.ShouldBeOfExactType<SimulatedStateTransitionError>();
    [Fact] void should_let_a_later_transition_run() => _laterFailure.ShouldBeNull();
    [Fact] void should_end_up_in_the_later_state() => _stateStorage.State.CurrentState.ShouldEqual(typeof(StateThatDoesNotChangeStateOnSelfTransition).FullName);
}
