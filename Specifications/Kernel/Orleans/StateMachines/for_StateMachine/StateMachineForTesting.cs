// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateMachineForTesting : StateMachine<StateMachineState>
{
    readonly Type _initialState;
    IImmutableList<IState<StateMachineState>> _states;

    public StateMachineForTesting(IEnumerable<IState<StateMachineState>> states, Type? initialState = default)
    {
        _states = states.ToImmutableList();
        _initialState = initialState;
    }

    protected override Type InitialState => _initialState ?? base.InitialState;

    public override IImmutableList<IState<StateMachineState>> CreateStates() => _states;
}
