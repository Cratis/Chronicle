// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IState{TStoredState}"/>.
/// </summary>
/// <typeparam name="TStoredState">Type of state object associated.</typeparam>
public abstract class State<TStoredState> : IState<TStoredState>
{
    /// <inheritdoc/>
    public abstract StateName Name { get; }

    /// <summary>
    /// Gets the supported state transitions from this state.
    /// </summary>
    protected virtual IImmutableList<Type> AllowedTransitions => ImmutableList<Type>.Empty;

    /// <inheritdoc/>
    public virtual Task<bool> CanTransitionTo<TTargetState>(TStoredState state) => throw new NotImplementedException();

    /// <inheritdoc/>
    public abstract Task<TStoredState> OnEnter(TStoredState state);

    /// <inheritdoc/>
    public abstract Task<TStoredState> OnLeave(TStoredState state);
}
