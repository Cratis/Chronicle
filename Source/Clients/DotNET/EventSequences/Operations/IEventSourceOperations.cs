// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
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
    /// <returns>The current instance of <see cref="EventSourceOperations"/>.</returns>
    EventSourceOperations Append(object @event, Causation? causation = default);

    /// <summary>
    /// Appends multiple events to the operation.
    /// </summary>
    /// <param name="events">The events to append.</param>
    /// <param name="causation">Optional causation for the events.</param>
    /// <returns>The current instance of <see cref="EventSourceOperations"/>.</returns>
    EventSourceOperations AppendMany(IEnumerable<object> events, Causation? causation = default);
}
