// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Defines operations related to an event sequence.
/// </summary>
public interface IEventSequenceOperations
{
    /// <summary>
    /// Gets the event sequence that these operations are associated with.
    /// </summary>
    IEventSequence EventSequence { get; }

    /// <summary>
    /// Configures operations for a specific event source identified by <paramref name="eventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId">The identifier of the event source.</param>
    /// <param name="configure">Action to configure the operations for the event source.</param>
    /// <returns>The current instance of <see cref="EventSequenceOperations"/>.</returns>
    EventSequenceOperations ForEventSourceId(EventSourceId eventSourceId, Action<EventSourceOperations> configure);

    /// <summary>
    /// Sets the causation for the operation.
    /// </summary>
    /// <param name="causation">The causation to set.</param>
    /// <returns>The current instance of <see cref="EventSequenceOperations"/>.</returns>
    EventSequenceOperations WithCausation(Causation causation);

    /// <summary>
    /// Gets the events that have been appended in the operation builders.
    /// </summary>
    /// <returns>Collection of events.</returns>
    IEnumerable<object> GetAppendedEvents();

    /// <summary>
    /// Clears all operations that has been added.
    /// </summary>
    void Clear();

    /// <summary>
    /// Performs the operation, appending events as specified in the builders.
    /// </summary>
    /// <returns>An instance of <see cref="AppendManyResult"/> representing the result of the operation.</returns>
    Task<AppendManyResult> Perform();
}
