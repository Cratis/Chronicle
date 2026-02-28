// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.StateMachines;

public class StateMachineForTesting(IEnumerable<IState<StateMachineStateForTesting>> states, Type? initialState = default) : StateMachine<StateMachineStateForTesting>
{
    readonly Type _initialState = initialState;
    IImmutableList<IState<StateMachineStateForTesting>> _states = states.ToImmutableList();

    public List<IState<StateMachineStateForTesting>> OnBeforeEnteringStates { get; } = [];
    public List<IState<StateMachineStateForTesting>> OnAfterEnteringStates { get; } = [];
    public List<IState<StateMachineStateForTesting>> OnBeforeLeavingStates { get; } = [];
    public List<IState<StateMachineStateForTesting>> OnAfterLeavingStates { get; } = [];

    protected override Type InitialState => _initialState ?? base.InitialState;

    public override IImmutableList<IState<StateMachineStateForTesting>> CreateStates() => _states;

    protected override Task OnBeforeEnteringState(IState<StateMachineStateForTesting> state)
    {
        if (state is NoOpState<StateMachineStateForTesting>)
        {
            return Task.CompletedTask;
        }

        OnBeforeEnteringStates.Add(state);
        return Task.CompletedTask;
    }

    protected override Task OnAfterEnteringState(IState<StateMachineStateForTesting> state)
    {
        if (state is NoOpState<StateMachineStateForTesting>)
        {
            return Task.CompletedTask;
        }

        OnAfterEnteringStates.Add(state);
        return Task.CompletedTask;
    }

    protected override Task OnBeforeLeavingState(IState<StateMachineStateForTesting> state)
    {
        if (state is NoOpState<StateMachineStateForTesting>)
        {
            return Task.CompletedTask;
        }

        OnBeforeLeavingStates.Add(state);
        return Task.CompletedTask;
    }

    protected override Task OnAfterLeavingState(IState<StateMachineStateForTesting> state)
    {
        if (state is NoOpState<StateMachineStateForTesting>)
        {
            return Task.CompletedTask;
        }

        OnAfterLeavingStates.Add(state);
        return Task.CompletedTask;
    }
}
