// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public abstract class BaseState : State<StateMachineState>
{
    public Action<StateMachineState> OnEnterCalled = _ => { };
    public Action<StateMachineState> OnLeaveCalled = _ => { };

    public StateMachineState StateToReturnOnEnter { get; set; } = new();
    public StateMachineState StateToReturnOnLeave { get; set; } = new();

    public override Task<StateMachineState> OnEnter(StateMachineState state)
    {
        OnEnterCalled(state);
        return Task.FromResult(StateToReturnOnEnter);
    }

    public override Task<StateMachineState> OnLeave(StateMachineState state)
    {
        OnLeaveCalled(state);
        return Task.FromResult(StateToReturnOnLeave);
    }
}
