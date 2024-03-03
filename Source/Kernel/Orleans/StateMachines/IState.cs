// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Defines a state in a <see cref="IStateMachine{T}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of state object associated.</typeparam>
public interface IState<TStoredState>
{
    /// <summary>
    /// Gets the state machine.
    /// </summary>
    IStateMachine<TStoredState> StateMachine { get; }

    /// <summary>
    /// Gets whether or not it is possible to transition to a given state.
    /// </summary>
    /// <param name="state">The state object associated.</param>
    /// <typeparam name="TTargetState">Type of state to transition to.</typeparam>
    /// <returns>True if it can transition, false it not.</returns>
    Task<bool> CanTransitionTo<TTargetState>(TStoredState state);

    /// <summary>
    /// Enter the state.
    /// </summary>
    /// <param name="state">The state object associated.</param>
    /// <returns>Updated state.</returns>
    Task<TStoredState> OnEnter(TStoredState state);

    /// <summary>
    /// Leave the state.
    /// </summary>
    /// <param name="state">The state object associated.</param>
    /// <returns>Updated state.</returns>
    Task<TStoredState> OnLeave(TStoredState state);
}
