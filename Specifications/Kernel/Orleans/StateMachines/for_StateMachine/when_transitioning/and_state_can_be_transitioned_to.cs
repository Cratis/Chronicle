// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_transitioning;

public class and_state_can_be_transitioned_to : given.a_state_machine_with_two_well_known_states
{
    protected override Type initial_state => typeof(StateThatDoesNotSupportTransitioning);
    List<(Type Type, StateMachineState State)> on_calls;
    StateMachineState initial_stored_state = new();

    void Establish()
    {
        on_calls = new();
        state_that_does_not_support_transitioning.OnEnterCalled = _ => on_calls.Add((typeof(StateThatDoesNotSupportTransitioning), _));
        state_that_supports_transitioning.OnEnterCalled = _ => on_calls.Add((typeof(StateThatSupportsTransitioning), _));
        state_that_does_not_support_transitioning.OnLeaveCalled = _ => on_calls.Add((typeof(StateThatDoesNotSupportTransitioning), _));
        state_that_supports_transitioning.OnLeaveCalled = _ => on_calls.Add((typeof(StateThatSupportsTransitioning), _));
        state_that_does_not_support_transitioning.StateToReturnOnLeave = new() { Something = "Leave State" };
        state_that_supports_transitioning.StateToReturnOnEnter = new() { Something = "Enter State" };
    }

    async Task Because() => await state_machine.TransitionTo<StateThatSupportsTransitioning>();

    [Fact] async Task should_transition_to_the_state() => (await state_machine.GetCurrentState()).ShouldBeOfExactType<StateThatSupportsTransitioning>();
    [Fact] void should_only_have_two_on_calls() => on_calls.Count.ShouldEqual(2);
    [Fact] void should_call_on_enter_on_the_state_transitioned_to() => on_calls[1].Type.ShouldEqual(typeof(StateThatSupportsTransitioning));
    [Fact] void should_call_on_enter_with_the_state_transitioned_to_and_state_from_left_state() => on_calls[1].State.ShouldEqual(state_that_does_not_support_transitioning.StateToReturnOnLeave);
    [Fact] void should_call_on_leave_on_the_state_transitioned_from() => on_calls[0].Type.ShouldEqual(typeof(StateThatDoesNotSupportTransitioning));
    [Fact] void should_call_on_leave_with_the_state_transitioned_from() => on_calls[0].State.ShouldEqual(initial_stored_state);
    [Fact] void should_write_state_once() => written_states.Count.ShouldEqual(1);
    [Fact] void should_write_state_coming_from_on_enter() => written_states[0].ShouldEqual(state_that_supports_transitioning.StateToReturnOnEnter);
    [Fact] void should_call_on_before_entering_state_for_state_entered() => state_machine.OnBeforeEnteringStates.ShouldContainOnly(new[] { state_that_supports_transitioning });
    [Fact] void should_call_on_after_entering_state_for_state_entered() => state_machine.OnAfterEnteringStates.ShouldContainOnly(new[] { state_that_supports_transitioning });
    [Fact] void should_call_on_before_leaving_state_for_state_entered() => state_machine.OnBeforeLeavingStates.ShouldContainOnly(new[] { state_that_does_not_support_transitioning });
    [Fact] void should_call_on_after_leaving_state_for_state_entered() => state_machine.OnAfterLeavingStates.ShouldContainOnly(new[] { state_that_does_not_support_transitioning });
}
