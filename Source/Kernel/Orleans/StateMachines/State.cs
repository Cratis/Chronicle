// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IState{TStoredState}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of state object associated.</typeparam>
public abstract class State<TStoredState> : IState<TStoredState>
{
    /// <summary>
    /// Internal field for <see cref="StateMachine"/>.
    /// </summary>
    internal IStateMachine<TStoredState> _stateMachine = default!;

    /// <inheritdoc/>
    public IStateMachine<TStoredState> StateMachine => _stateMachine;

    /// <summary>
    /// Gets the supported state transitions from this state.
    /// </summary>
    protected virtual IImmutableList<Type> AllowedTransitions => ImmutableList<Type>.Empty;

    /// <inheritdoc/>
    public virtual Task<bool> CanTransitionTo<TTargetState>(TStoredState state) => Task.FromResult(AllowedTransitions.Contains(typeof(TTargetState)));

    /// <inheritdoc/>
    public abstract Task<TStoredState> OnEnter(TStoredState state);

    /// <inheritdoc/>
    public abstract Task<TStoredState> OnLeave(TStoredState state);
}
