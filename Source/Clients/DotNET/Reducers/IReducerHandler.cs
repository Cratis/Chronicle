// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Defines a system that handle events for a specific reducer.
/// </summary>
public interface IReducerHandler
{
    /// <summary>
    /// Gets the unique identifier of the reducer.
    /// </summary>
    ReducerId ReducerId { get; }

    /// <summary>
    /// Gets the name of the reducer.
    /// </summary>
    ReducerName Name { get; }

    /// <summary>
    /// Gets the event sequence the reducer is reducing from.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the event types for the reducer.
    /// </summary>
    IEnumerable<EventType> EventTypes { get; }

    /// <summary>
    /// Handle next event.
    /// </summary>
    /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
    /// <param name="initial">Initial read model value.</param>
    /// <returns>Reduced read model.</returns>
    Task<object> OnNext(AppendedEvent @event, object? initial);

    /// <summary>
    /// Handle next events as bulk.
    /// </summary>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to handle.</param>
    /// <param name="initial">Initial read model value.</param>
    /// <returns>Reduced read model.</returns>
    Task<object> OnNextBulk(IEnumerable<AppendedEvent> events, object? initial);
}
