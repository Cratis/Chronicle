// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.given;

public abstract class a_state_machine : GrainSpecification<StateMachineState>
{
    protected StateMachineForTesting state_machine;

    protected override Guid GrainId => Guid.Empty;

    protected override string GrainKeyExtension => string.Empty;

    protected virtual Type? initial_state => default;

    protected override Grain GetGrainInstance()
    {
        state_machine = new(CreateStates(), initial_state);
        return state_machine;
    }

    protected abstract IEnumerable<IState<StateMachineState>> CreateStates();
}
