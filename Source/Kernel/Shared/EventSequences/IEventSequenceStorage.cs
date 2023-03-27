// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Defines a storage provider for the event sequence.
/// </summary>
public interface IEventSequenceStorage
{
    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> representing the event sequence to append to.</param>
    /// <param name="sequenceNumber">The unique <see cref="EventSequenceNumber">sequence number</see> within the event sequence.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <param name="content">The content of the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, DateTimeOffset validFrom, ExpandoObject content);

    /// <summary>
    /// Compensate a single event to the event store.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> representing the event sequence to append to.</param>
    /// <param name="sequenceNumber">The unique <see cref="EventSequenceNumber">sequence number</see> within the event sequence.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <param name="content">The content of the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Compensate(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventType eventType, DateTimeOffset validFrom, ExpandoObject content);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> representing the event sequence to redact from.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <returns>Affected event.</returns>
    Task<AppendedEvent> Redact(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, RedactionReason reason);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> representing the event sequence to redact from.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// /// <param name="reason">Reason for redacting.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <returns>Affected event types.</returns>
    Task<IEnumerable<EventType>> Redact(EventSequenceId eventSequenceId, EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType>? eventTypes);

    /// <summary>
    /// Get the sequence number of the first event as part of the filtered event types.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>The last sequence number. If providing event types, this will give the last sequence number from the selection of event types.</returns>
    Task<EventSequenceNumber> GetHeadSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Get the sequence number of the last event as part of the filtered event types.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>The last sequence number. If providing event types, this will give the last sequence number from the selection of event types.</returns>
    Task<EventSequenceNumber> GetTailSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Get the next sequence number greater or equal to a specific sequence number with optionally filtered on event types and event source id.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="sequenceNumber">The sequence number to search from.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>
    /// <p>The last sequence number.</p>
    /// <p>If providing event types, this will give the last sequence number from the selection of event types.</p>
    /// <p>If no event is found, it will return <see cref="EventSequenceNumber.Unavailable"/>.</p>
    /// </returns>
    Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Check if there is an instance of a specific event type for an event source.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<bool> HasInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId);

    /// <summary>
    /// Gets the event at a specific sequence number.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="sequenceNumber">The sequence number the event is at.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetEventAt(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Get the last instance of a specific event type for an event source.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetLastInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId);

    /// <summary>
    /// Get the last instance of any of the specified event types for a specific event source.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <param name="eventTypes">Any in the collection of <see cref="EventTypeId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetLastInstanceOfAny(EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes);

    /// <summary>
    /// Get events using a specific sequence number as starting point within the event sequence.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> to get from.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetFromSequenceNumber(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);

    /// <summary>
    /// Get events within a specific sequence number range.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence to get for.</param>
    /// <param name="start">Start of the range.</param>
    /// <param name="end">End of the range.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetRange(EventSequenceId eventSequenceId, EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default);
}
