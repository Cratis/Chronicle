// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateMachineWithUnknownInitialStateType : StateMachine<StateMachineState>
{
    protected override Type InitialState => typeof(StateThatDoesNotSupportTransitioningFrom);

    public override ImmutableList<IState<StateMachineState>> CreateStates() =>
        new IState<StateMachineState>[]
        {
            new StateThatSupportsTransitioningFrom()
        }.ToImmutableList();
}
