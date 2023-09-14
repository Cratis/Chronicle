// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateThatTransitionsOnLeave : BaseState
{
    public override StateName Name => "State that transitions on enter";

    public override Task<bool> CanTransitionTo<TState>(StateMachineState state) => Task.FromResult(true);

    public override async Task<StateMachineState> OnLeave(StateMachineState state)
    {
        await StateMachine.TransitionTo<StateThatSupportsTransitioning>();
        return await base.OnLeave(state);
    }
}
