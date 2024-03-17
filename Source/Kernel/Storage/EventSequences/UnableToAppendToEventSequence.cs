// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Kernel.Storage.EventSequences;

/// <summary>
/// Exception that gets thrown when the storage mechanism is not able to append the event to the event sequence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnableToAppendToEventSequence"/> class.
/// </remarks>
/// <param name="eventSequenceId">The stream that is failing.</param>
/// <param name="eventStore">For which event store it is failing for.</param>
/// <param name="namespace">For which namespace it is failing for.</param>
/// <param name="sequenceNumber">The sequence number that is failing.</param>
/// <param name="eventSourceId">EventSource it is failing for.</param>
/// <param name="innerException">The inner exception.</param>
public class UnableToAppendToEventSequence(
    EventSequenceId eventSequenceId,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceNumber sequenceNumber,
    EventSourceId eventSourceId,
    Exception innerException)
    : Exception($"Unable to append event at sequence {sequenceNumber} for event source {eventSourceId} in namespace {@namespace} in event store {eventStore} from event sequence {eventSequenceId}", innerException)
{
    /// <summary>
    /// Gets the stream identifier.
    /// </summary>
    public Guid StreamId { get; } = eventSequenceId;

    /// <summary>
    /// Gets the namespace.
    /// </summary>
    public EventStoreNamespaceName Namespace { get; } = @namespace;

    /// <summary>
    /// Gets the sequence number within the event sequence.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; } = sequenceNumber;

    /// <summary>
    /// Gets the event source identifier.
    /// </summary>
    public EventSourceId EventSourceId { get; } = eventSourceId;
}
