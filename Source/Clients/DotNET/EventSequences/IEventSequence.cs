// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Defines the client event sequence.
/// </summary>
public interface IEventSequence
{
    /// <summary>
    /// Get all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> eventTypes);

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
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// <param name="validFrom">Optional date and time for when the event is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Append a collection of events to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="events">Collection of events to append.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task AppendMany(EventSourceId eventSourceId, IEnumerable<object> events);

    /// <summary>
    /// Append a collection of events to the event store with valid from per event.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="events">Collection of events with valid from to append.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task AppendMany(EventSourceId eventSourceId, IEnumerable<EventAndValidFrom> events);

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
