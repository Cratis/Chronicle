// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Represents a base implementation of <see cref="IStateMachine{TStoredState}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of stored state.</typeparam>
public class StateMachine<TStoredState> : Grain<TStoredState>, IStateMachine<TStoredState>
{
    /// <inheritdoc/>
    public Task<bool> CanTransitionTo<TState>()
        where TState : IState<TStoredState> => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task TransitionTo<TState>()
        where TState : IState<TStoredState> => throw new NotImplementedException();
}
