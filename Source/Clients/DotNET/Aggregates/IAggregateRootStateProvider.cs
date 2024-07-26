// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines a system that can manage state for an <see cref="AggregateRoot"/>.
/// </summary>
/// <typeparam name="TState">Type of state to manage.</typeparam>
public interface IAggregateRootStateProvider<TState>
    where TState : class
{
    /// <summary>
    /// Handle state for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <returns>State provided.</returns>
    Task<TState?> Provide();

    /// <summary>
    /// Update the state of an <see cref="AggregateRoot"/> with events.
    /// </summary>
    /// <param name="initialState">The initial state to update from.</param>
    /// <param name="events">The events to update with.</param>
    /// <returns>Updated state.</returns>
    Task<TState?> Update(TState? initialState, IEnumerable<object> events);

    /// <summary>
    /// Dehydrate any state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Dehydrate();
}
