// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_asking_can_transition_to;

public record StateMachineState();

public class StateThatSupportsTransitioning : State<StateMachineState>
{
    public override StateName Name => "No transitioning state";

    public override Task<StateMachineState> OnEnter(StateMachineState state) => Task.FromResult(state);

    public override Task<StateMachineState> OnLeave(StateMachineState state) => Task.FromResult(state);

    public override Task<bool> CanTransitionTo<TState>(StateMachineState state) => Task.FromResult(true);
}


public class StateThatDoesNotSupportTransitioning : State<StateMachineState>
{
    public override StateName Name => "Transitioning state";

    public override Task<StateMachineState> OnEnter(StateMachineState state) => Task.FromResult(state);

    public override Task<StateMachineState> OnLeave(StateMachineState state) => Task.FromResult(state);

    public override Task<bool> CanTransitionTo<TState>(StateMachineState state) => Task.FromResult(false);
}

public class StateMachineForTesting : StateMachine<StateMachineState>
{
    IImmutableList<IState<StateMachineState>> _states;


    public StateMachineForTesting(IEnumerable<IState<StateMachineState>> states) => _states = states.ToImmutableList();

    public override IImmutableList<IState<StateMachineState>> GetStates() => _states;
}

public abstract class a_state_machine : GrainSpecification<StateMachineState>
{
    protected StateMachineForTesting state_machine;

    protected override Guid GrainId => Guid.Empty;

    protected override string GrainKeyExtension => string.Empty;

    protected override Grain GetGrainInstance()
    {
        state_machine = new(GetStates());
        return state_machine;
    }

    protected abstract IEnumerable<IState<StateMachineState>> GetStates();
}


public class and_state_does_not_allow_it : a_state_machine
{
    bool result;

    protected override IEnumerable<IState<StateMachineState>> GetStates() => new IState<StateMachineState>[]
    {
        new StateThatSupportsTransitioning(),
        new StateThatDoesNotSupportTransitioning()
    };

    async Task Because() => result = await state_machine.CanTransitionTo<StateThatDoesNotSupportTransitioning>();

    [Fact] void should_not_be_able_to_transition() => result.ShouldBeFalse();
}
