// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateMachineForTesting : StateMachine<StateMachineState>
{
    IImmutableList<IState<StateMachineState>> _states;

    public StateMachineForTesting(IEnumerable<IState<StateMachineState>> states) => _states = states.ToImmutableList();

    public override IImmutableList<IState<StateMachineState>> GetStates() => _states;
}
