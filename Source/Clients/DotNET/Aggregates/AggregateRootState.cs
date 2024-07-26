// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootState{TState}"/>.
/// </summary>
/// <typeparam name="TState">Type of state.</typeparam>
public class AggregateRootState<TState> : IAggregateRootState<TState>, IAggregateRootStateModifier<TState>
    where TState : class
{
    /// <inheritdoc/>
    public TState State { get; private set; } = default!;

    /// <inheritdoc/>
    public void SetState(TState state) => State = state;
}
