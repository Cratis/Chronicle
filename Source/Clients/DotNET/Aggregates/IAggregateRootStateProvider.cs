// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines a system that can manage state for an <see cref="AggregateRoot"/>.
/// </summary>
public interface IAggregateRootStateProvider
{
    /// <summary>
    /// Handle state for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <returns>State provided.</returns>
    Task<object?> Provide();

    /// <summary>
    /// Update the state of an <see cref="AggregateRoot"/> with events.
    /// </summary>
    /// <param name="initialState">The initial state to update from.</param>
    /// <param name="events">The events to update with.</param>
    /// <returns>Updated state.</returns>
    Task<object?> Update(object? initialState, IEnumerable<object> events);

    /// <summary>
    /// Dehydrate any state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Dehydrate();
}
