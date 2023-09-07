// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateMachineWithInvalidInitialStateType : StateMachine<StateMachineState>
{
    protected override Type InitialState => typeof(string);

    public override IImmutableList<IState<StateMachineState>> GetStates() => ImmutableList<IState<StateMachineState>>.Empty;
}
