// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class UnknownState : BaseState
{
    public override Task<bool> CanTransitionTo<TState>(StateMachineState state) => Task.FromResult(true);
}
