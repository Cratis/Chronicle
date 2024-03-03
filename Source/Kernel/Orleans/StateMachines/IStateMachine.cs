// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Defines a state machine.
/// </summary>
/// <typeparam name="TStoredState">Type of stored state.</typeparam>
public interface IStateMachine<TStoredState>
{
    /// <summary>
    /// Gets the current state.
    /// </summary>
    /// <returns>The current state.</returns>
    Task<IState<TStoredState>> GetCurrentState();

    /// <summary>
    /// Get all the states.
    /// </summary>
    /// <returns>A collection of state instances.</returns>
    Task<IImmutableList<IState<TStoredState>>> GetStates();

    /// <summary>
    /// Check if it can transition to a specific new state.
    /// </summary>
    /// <typeparam name="TState">Type of state to check.</typeparam>
    /// <returns>True if it can, false if not.</returns>
    Task<bool> CanTransitionTo<TState>()
        where TState : IState<TStoredState>;

    /// <summary>
    /// Transition to a new state.
    /// </summary>
    /// <typeparam name="TState">Type of state to transition to.</typeparam>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task TransitionTo<TState>()
        where TState : IState<TStoredState>;
}
