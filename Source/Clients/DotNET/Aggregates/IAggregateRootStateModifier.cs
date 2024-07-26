// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines an internal interface for modifying the state of an <see cref="IAggregateRoot"/>.
/// </summary>
/// <typeparam name="TState">Type of state.</typeparam>
/// <remarks>
/// Typically used by <see cref="AggregateRootState{TState}"/>.
/// </remarks>
internal interface IAggregateRootStateModifier<TState>
{
    /// <summary>
    /// Set the state of the aggregate root.
    /// </summary>
    /// <param name="state">State to set.</param>
    void SetState(TState state);
}
