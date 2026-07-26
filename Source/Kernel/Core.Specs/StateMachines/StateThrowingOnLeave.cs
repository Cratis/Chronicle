// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines;

/// <summary>
/// A state whose <see cref="OnLeave"/> fails while <see cref="ShouldThrow"/> is set, standing in for the unsubscribe
/// and storage calls real states make there.
/// </summary>
public class StateThrowingOnLeave : BaseState
{
    public bool ShouldThrow { get; set; } = true;

    public override Task<bool> CanTransitionTo<TState>(StateMachineStateForTesting state) => Task.FromResult(true);

    public override Task<StateMachineStateForTesting> OnLeave(StateMachineStateForTesting state) =>
        ShouldThrow ? throw new SimulatedStateTransitionError() : base.OnLeave(state);
}
