// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the client event sequence.
/// </summary>
public interface IEventSequence
{
    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> for the event sequence.
    /// </summary>
    EventSequenceId Id { get; }

    /// <summary>
    /// Get all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> eventTypes, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default);

    /// <summary>
    /// Check if there are events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to check for.</param>
    /// <returns>True if it has, false if not.</returns>
    Task<bool> HasEventsFor(EventSourceId eventSourceId);

    /// <summary>
    /// Get all events after and including the given <see cref="EventSequenceNumber"/> with optional <see cref="EventSourceId"/> and <see cref="IEnumerable{T}"/> of <see cref="EventType"/> for filtering.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the first event to get from.</param>
    /// <param name="eventSourceId">The optional <see cref="EventSourceId"/>.</param>
    /// <param name="eventTypes">The optional <see cref="IEnumerable{T}"/> of <see cref="EventType"/>.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);

    /// <summary>
    /// Get the next sequence number.
    /// </summary>
    /// <returns>Next sequence number.</returns>
    Task<EventSequenceNumber> GetNextSequenceNumber();

    /// <summary>
    /// Get the sequence number of the last (tail) event in the sequence.
    /// </summary>
    /// <returns>Tail sequence number.</returns>
    Task<EventSequenceNumber> GetTailSequenceNumber();

    /// <summary>
    /// Get the sequence number of the last (tail) event in the sequence for a specific observer.
    /// </summary>
    /// <param name="type">Type of observer to get for.</param>
    /// <returns>Tail sequence number.</returns>
    /// <remarks>
    /// This is based on the tail of the event types the observer is interested in.
    /// </remarks>
    Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type);

    /// <summary>
    /// Append a single event to the event store as a transaction.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event. Defaults to <see cref="ICorrelationIdAccessor.Current"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default);

    /// <summary>
    /// Append a collection of events to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="events">Collection of events to append.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to append to. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to append to. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to append to. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event. Defaults to <see cref="ICorrelationIdAccessor.Current"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// All events will be committed as one operation for the underlying data store.
    /// </remarks>
    Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default);

    /// <summary>
    /// Append a collection of events to the event store as a transaction.
    /// </summary>
    /// <param name="events">Collection of <see cref="EventForEventSourceId"/> to append.</param>
    /// <param name="correlationId">Optional <see cref="CorrelationId"/> of the event. Defaults to <see cref="ICorrelationIdAccessor.Current"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// All events will be committed as one operation for the underlying data store.
    /// </remarks>
    Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events, CorrelationId? correlationId = default);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Optional reason for redacting. Will default to <see cref="RedactionReason.Unknown"/> if not specified.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Redact(EventSequenceNumber sequenceNumber, RedactionReason? reason = default);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Optional reason for redacting. Will default to <see cref="RedactionReason.Unknown"/> if not specified.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Redact(EventSourceId eventSourceId, RedactionReason? reason = default, params Type[] eventTypes);
}
