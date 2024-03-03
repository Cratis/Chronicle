// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Represents a no operation state.
/// </summary>
/// <typeparam name="TStoredState">Type of stored state.</typeparam>
public class NoOpState<TStoredState> : State<TStoredState>
{
    /// <inheritdoc/>
    public override Task<bool> CanTransitionTo<TTargetState>(TStoredState state) => Task.FromResult(true);

    /// <inheritdoc/>
    public override Task<TStoredState> OnEnter(TStoredState state) => Task.FromResult(state);

    /// <inheritdoc/>
    public override Task<TStoredState> OnLeave(TStoredState state) => Task.FromResult(state);
}
