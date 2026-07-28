// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.when_transitioning;

/// <summary>
/// States do storage and grain calls on enter, so entering can fail. If the in-transition flag were left raised the
/// machine would take every later transition as one to schedule and never run it - it could never change state
/// again, and a kept-alive grain is never reactivated out of that.
/// </summary>
public class and_on_enter_throws : given.a_state_machine_with_failing_states
{
    protected override Type InitialState => typeof(StateThatSupportsTransitioningFrom);

    Exception? _failure;
    Exception? _laterFailure;

    async Task Because()
    {
        _failure = await TransitionTo<StateThrowingOnEnter>();
        _laterFailure = await TransitionTo<StateThatDoesNotChangeStateOnSelfTransition>();
    }

    [Fact] void should_surface_the_failure_to_the_caller() => _failure.ShouldBeOfExactType<SimulatedStateTransitionError>();
    [Fact] void should_let_a_later_transition_run() => _laterFailure.ShouldBeNull();
    [Fact] void should_end_up_in_the_later_state() => _stateStorage.State.CurrentState.ShouldEqual(typeof(StateThatDoesNotChangeStateOnSelfTransition).FullName);
}
