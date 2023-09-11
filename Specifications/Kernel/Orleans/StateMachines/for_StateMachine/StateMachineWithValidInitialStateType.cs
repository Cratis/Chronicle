// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateMachineWithValidInitialStateType : StateMachine<StateMachineState>
{
    protected override Type InitialState => typeof(StateThatSupportsTransitioning);

    public override IImmutableList<IState<StateMachineState>> CreateStates() => ImmutableList<IState<StateMachineState>>.Empty;
}
