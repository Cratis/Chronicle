// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_activating;

public class with_known_initial_state_type : given.a_state_machine
{
    StateMachineState on_enter_called_state;

    protected override Type initial_state => typeof(StateThatSupportsTransitioningFrom);

    protected override IEnumerable<IState<StateMachineState>> CreateStates() => new IState<StateMachineState>[]
        {
            new StateThatDoesNotSupportTransitioningFrom(),
            new StateThatSupportsTransitioningFrom { OnEnterCalled = _ => on_enter_called_state = _ }
        };

    async Task Because() => await state_machine.OnActivateAsync(CancellationToken.None);

    [Fact] async Task should_set_current_state_to_expected_initial_state_type() => (await state_machine.GetCurrentState()).ShouldBeAssignableFrom<StateThatSupportsTransitioningFrom>();
    [Fact] void should_call_on_enter_with_expected_state() => on_enter_called_state.ShouldEqual(state_storage.State);
    [Fact] async Task should_set_state_machine_on_all_states() => (await state_machine.GetStates()).All(_ => _.StateMachine.Equals(state_machine)).ShouldBeTrue();
}
