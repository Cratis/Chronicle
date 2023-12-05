// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_transitioning;

public class and_state_can_be_transitioned_to : given.a_state_machine_with_well_known_states
{
    protected override Type initial_state => typeof(StateThatSupportsTransitioningFrom);

    void Establish()
    {
        // We clear this, because we don't care about the initial state transitions and state written to storage in this spec
        _ = state_machine;  // Since the state machine is lazily created, we need an instance of it before we can clear states since it performs operations on creation that we record
        silo.StorageStats().ResetCounts();
        on_calls.Clear();

        state_machine.OnBeforeEnteringStates.Clear();
        state_machine.OnAfterEnteringStates.Clear();
    }

    async Task Because() => await state_machine.TransitionTo<StateThatDoesNotSupportTransitioningFrom>();

    [Fact] async Task should_transition_to_the_state() => (await state_machine.GetCurrentState()).ShouldBeOfExactType<StateThatDoesNotSupportTransitioningFrom>();
    [Fact] void should_only_have_two_on_calls() => on_calls.Count.ShouldEqual(2);
    [Fact] void should_call_the_state_transitioned_to() => on_calls[1].Type.ShouldEqual(typeof(StateThatDoesNotSupportTransitioningFrom));
    [Fact] void should_call_on_enter_on_the_state_transitioned_to() => on_calls[1].IsEnter.ShouldBeTrue();
    [Fact] void should_call_on_enter_with_the_state_transitioned_to_and_state_from_left_state() => on_calls[1].State.ShouldEqual(state_that_supports_transitioning.StateToReturnOnLeave);
    [Fact] void should_call_the_state_transitioned_from() => on_calls[0].Type.ShouldEqual(typeof(StateThatSupportsTransitioningFrom));
    [Fact] void should_call_on_leave_on_the_state_transitioned_from() => on_calls[0].IsEnter.ShouldBeFalse();
    [Fact] void should_call_on_leave_with_the_state_transitioned_from() => on_calls[0].State.ShouldEqual(state_that_supports_transitioning.StateToReturnOnEnter);
    [Fact] void should_write_state_once() => silo.StorageStats().Writes.ShouldEqual(1);
    [Fact] void should_write_state_coming_from_on_enter() => state_storage.State.ShouldEqual(state_that_does_not_support_transitioning.StateToReturnOnEnter);
    [Fact] void should_call_on_before_entering_state_for_state_entered() => state_machine.OnBeforeEnteringStates.ShouldContainOnly(new[] { state_that_does_not_support_transitioning });
    [Fact] void should_call_on_after_entering_state_for_state_entered() => state_machine.OnAfterEnteringStates.ShouldContainOnly(new[] { state_that_does_not_support_transitioning });
    [Fact] void should_call_on_before_leaving_state_for_state_entered() => state_machine.OnBeforeLeavingStates.ShouldContainOnly(new[] { state_that_supports_transitioning });
    [Fact] void should_call_on_after_leaving_state_for_state_entered() => state_machine.OnAfterLeavingStates.ShouldContainOnly(new[] { state_that_supports_transitioning });
}
