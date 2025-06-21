// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Defines operations related to a specific event source within an event sequence.
/// </summary>
public interface IEventSourceOperations
{
    /// <summary>
    /// Gets the operations associated with the event source.
    /// </summary>
    IEnumerable<IEventSequenceOperation> Operations { get; }

    /// <summary>
    /// Gets the concurrency scope for the operations.
    /// </summary>
    ConcurrencyScope ConcurrencyScope { get; }

    /// <summary>
    /// Configures the concurrency scope for the operations.
    /// </summary>
    /// <param name="configure">Action to configure the concurrency scope.</param>
    /// <returns>The current instance of <see cref="EventSourceOperations"/>.</returns>
    EventSourceOperations WithConcurrencyScope(Action<ConcurrencyScopeBuilder> configure);

    /// <summary>
    /// Appends an event to the operation.
    /// </summary>
    /// <param name="event">The event to append.</param>
    /// <param name="causation">Optional causation for the event.</param>
    /// <param name="eventStreamType">Optional The type of event stream.</param>
    /// <param name="eventStreamId">Optional The identifier for the event stream.</param>
    /// <param name="eventSourceType">Optional The type of event source.</param>
    /// <returns>The current instance of <see cref="EventSourceOperations"/>.</returns>
    EventSourceOperations Append(
        object @event,
        Causation? causation = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default);

    /// <summary>
    /// Gets the operations of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of operations to retrieve.</typeparam>
    /// <returns>Collection of operations of the specified type.</returns>
    IEnumerable<T> GetOperationsOfType<T>()
        where T : IEventSequenceOperation;

    /// <summary>
    /// Gets the events that have been appended in the operation builders.
    /// </summary>
    /// <returns>Collection of events.</returns>
    IEnumerable<object> GetAppendedEvents();
}
