// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateThatTransitionsOnEnter : BaseState
{
    public override Task<bool> CanTransitionTo<TState>(StateMachineState state) => Task.FromResult(true);

    public override async Task<StateMachineState> OnEnter(StateMachineState state)
    {
        await StateMachine.TransitionTo<StateThatTransitionsOnLeave>();
        return await base.OnEnter(state);
    }
}
