// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines the state provider for an <see cref="IAggregateRoot"/>.
/// </summary>
/// <typeparam name="TState">Type of state.</typeparam>
public interface IAggregateRootState<TState>
{
    /// <summary>
    /// Gets the current state for the aggregate root.
    /// </summary>
    TState State { get; }
}
