// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_transitioning;

public class and_state_can_be_transitioned_to : given.a_state_machine_with_two_well_known_states
{
    protected override Type initial_state => typeof(StateThatDoesNotSupportTransitioning);
    List<(Type Type, StateMachineState State)> on_calls;

    void Establish()
    {
        on_calls = new();
        state_that_does_not_support_transitioning.OnEnterCalled = _ => on_calls.Add((typeof(StateThatDoesNotSupportTransitioning), _));
        state_that_supports_transitioning.OnEnterCalled = _ => on_calls.Add((typeof(StateThatSupportsTransitioning), _));
        state_that_does_not_support_transitioning.OnLeaveCalled = _ => on_calls.Add((typeof(StateThatDoesNotSupportTransitioning), _));
        state_that_supports_transitioning.OnLeaveCalled = _ => on_calls.Add((typeof(StateThatSupportsTransitioning), _));
    }

    async Task Because() => await state_machine.TransitionTo<StateThatSupportsTransitioning>();

    [Fact] async Task should_transition_to_the_state() => (await state_machine.GetCurrentState()).ShouldBeOfExactType<StateThatSupportsTransitioning>();
    [Fact] void should_only_have_two_on_calls() => on_calls.Count.ShouldEqual(2);
    [Fact] void should_call_on_enter_on_the_state_transitioned_to() => on_calls[1].Type.ShouldEqual(typeof(StateThatSupportsTransitioning));
    [Fact] void should_call_on_enter_with_the_state_transitioned_to() => on_calls[1].State.ShouldEqual(state);
    [Fact] void should_call_on_leave_on_the_state_transitioned_from() => on_calls[0].Type.ShouldEqual(typeof(StateThatDoesNotSupportTransitioning));
    [Fact] void should_call_on_leave_with_the_state_transitioned_from() => on_calls[0].State.ShouldEqual(state);
}
