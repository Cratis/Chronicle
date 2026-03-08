// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines;

public class StateThatTransitionsOnLeave : BaseState
{
    public override Task<bool> CanTransitionTo<TState>(StateMachineStateForTesting state) => Task.FromResult(true);

    public override async Task<StateMachineStateForTesting> OnLeave(StateMachineStateForTesting state)
    {
        await StateMachine.TransitionTo<StateThatSupportsTransitioningFrom>();
        return await base.OnLeave(state);
    }
}
