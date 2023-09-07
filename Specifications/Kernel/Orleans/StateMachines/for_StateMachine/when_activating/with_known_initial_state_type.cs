// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_activating;

public class with_known_initial_state_type : GrainSpecification<StateMachineState>
{
    protected override Guid GrainId => Guid.Empty;
    protected override string GrainKeyExtension => string.Empty;
    StateMachineState on_enter_called_state;

    protected override Grain GetGrainInstance() => new StateMachineForTesting(
        new IState<StateMachineState>[]
        {
            new StateThatDoesNotSupportTransitioning(),
            new StateThatSupportsTransitioning { OnEnterCalled = _ => on_enter_called_state = _ }
        },
        typeof(StateThatSupportsTransitioning));

    async Task Because() => await grain.OnActivateAsync(CancellationToken.None);

    [Fact] async Task should_set_current_state_to_expected_initial_state_type() => (await ((StateMachineForTesting)grain).GetCurrentState()).ShouldBeAssignableFrom<StateThatSupportsTransitioning>();
    [Fact] void should_call_on_enter_with_expected_state() => on_enter_called_state.ShouldEqual(state);
}
