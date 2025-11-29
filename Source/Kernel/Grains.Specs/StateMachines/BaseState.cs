// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.StateMachines;

public abstract class BaseState : State<StateMachineStateForTesting>
{
    public Action<StateMachineStateForTesting> OnEnterCalled = _ => { };
    public Action<StateMachineStateForTesting> OnLeaveCalled = _ => { };

    public StateMachineStateForTesting StateToReturnOnEnter { get; set; } = new();
    public StateMachineStateForTesting StateToReturnOnLeave { get; set; } = new();

    public override Task<StateMachineStateForTesting> OnEnter(StateMachineStateForTesting state)
    {
        OnEnterCalled(state);
        return Task.FromResult(StateToReturnOnEnter);
    }

    public override Task<StateMachineStateForTesting> OnLeave(StateMachineStateForTesting state)
    {
        OnLeaveCalled(state);
        return Task.FromResult(StateToReturnOnLeave);
    }
}
