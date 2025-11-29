// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.StateMachines;

public class StateThatTransitionsOnEnter : BaseState
{
    public override Task<bool> CanTransitionTo<TState>(StateMachineStateForTesting state) => Task.FromResult(true);

    public override async Task<StateMachineStateForTesting> OnEnter(StateMachineStateForTesting state)
    {
        await StateMachine.TransitionTo<StateThatTransitionsOnLeave>();
        return await base.OnEnter(state);
    }
}
