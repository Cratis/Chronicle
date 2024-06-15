// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Defines a storage provider for the event sequence.
/// </summary>
public interface IEventSequenceStorage
{
    /// <summary>
    /// Get the state of an event sequence.
    /// </summary>
    /// <returns><see cref="EventSequenceState"/> for the event sequence.</returns>
    Task<EventSequenceState> GetState();

    /// <summary>
    /// Save state for an event sequence.
    /// </summary>
    /// <param name="state">The <see cref="EventSequenceState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task SaveState(EventSequenceState state);

    /// <summary>
    /// Get count of events in an event sequence.
    /// </summary>
    /// <param name="lastEventSequenceNumber">Optional last sequence number to count.</param>
    /// <param name="eventTypes">Event types to count for.</param>
    /// <returns>Total number of events.</returns>
    Task<EventCount> GetCount(EventSequenceNumber? lastEventSequenceNumber = null, IEnumerable<EventType>? eventTypes = null);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="sequenceNumber">The unique <see cref="EventSequenceNumber">sequence number</see> within the event sequence.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedByChain">The chain of <see cref="IdentityId"/> representing the person, system or service that caused the event.</param>
    /// <param name="occurred">The date and time the event occurred.</param>
    /// <param name="validFrom">Date and time for when the compensation is valid from. </param>
    /// <param name="content">The content of the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred, DateTimeOffset validFrom, ExpandoObject content);

    /// <summary>
    /// Compensate a single event to the event store.
    /// </summary>
    /// <param name="sequenceNumber">The unique <see cref="EventSequenceNumber">sequence number</see> within the event sequence.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedByChain">The chain of <see cref="IdentityId"/> representing the person, system or service that caused the event.</param>
    /// <param name="occurred">The date and time the compensation occurred.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <param name="content">The content of the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Compensate(EventSequenceNumber sequenceNumber, EventType eventType, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred, DateTimeOffset validFrom, ExpandoObject content);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedByChain">The chain of <see cref="IdentityId"/> representing the person, system or service that caused the event.</param>
    /// <param name="occurred">The date and time the redaction occurred.</param>
    /// <returns>Affected event.</returns>
    Task<AppendedEvent> Redact(EventSequenceNumber sequenceNumber, RedactionReason reason, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedByChain">The chain of <see cref="IdentityId"/> representing the person, system or service that caused the event.</param>
    /// <param name="occurred">The date and time the redaction occurred.</param>
    /// <returns>Affected event types.</returns>
    Task<IEnumerable<EventType>> Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType>? eventTypes, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred);

    /// <summary>
    /// Get the sequence number of the first event as part of the filtered event types.
    /// </summary>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>The last sequence number. If providing event types, this will give the last sequence number from the selection of event types.</returns>
    Task<EventSequenceNumber> GetHeadSequenceNumber(IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Get the sequence number of the last event as part of the filtered event types.
    /// </summary>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>The last sequence number. If providing event types, this will give the last sequence number from the selection of event types.</returns>
    Task<EventSequenceNumber> GetTailSequenceNumber(IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Get the tail sequence numbers for a specific event sequence and any of a given set of event types.
    /// </summary>
    /// <param name="eventTypes">Event types to get for.</param>
    /// <returns>The <see cref="TailEventSequenceNumbers"/>.</returns>
    Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Get the sequence number of the last event as part for given event types.
    /// </summary>
    /// <param name="eventTypes">Event types to get for.</param>
    /// <returns>A dictionary of <see cref="EventType"/> tp <see cref="EventSequenceNumber"/>.</returns>
    Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Get the next sequence number greater or equal to a specific sequence number with optionally filtered on event types and event source id.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number to search from.</param>
    /// <param name="eventTypes">Optional event types to get for.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to get for. It won't filter by this if omitted.</param>
    /// <returns>
    /// <p>The last sequence number.</p>
    /// <p>If providing event types, this will give the last sequence number from the selection of event types.</p>
    /// <p>If no event is found, it will return <see cref="EventSequenceNumber.Unavailable"/>.</p>
    /// </returns>
    Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber sequenceNumber, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null);

    /// <summary>
    /// Check if there is an instance of a specific event type for an event source.
    /// </summary>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<bool> HasInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId);

    /// <summary>
    /// Gets the event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number the event is at.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Get the last instance of a specific event type for an event source.
    /// </summary>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetLastInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId);

    /// <summary>
    /// Get the last instance of any of the specified event types for a specific event source.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to get for.</param>
    /// <param name="eventTypes">Any in the collection of <see cref="EventTypeId"/> to get for.</param>
    /// <returns>The <see cref="AppendedEvent"/> found.</returns>
    Task<AppendedEvent> GetLastInstanceOfAny(EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes);

    /// <summary>
    /// Get events using a specific sequence number as starting point within the event sequence.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> to get from.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events within a specific sequence number range.
    /// </summary>
    /// <param name="start">Start of the range.</param>
    /// <param name="end">End of the range.</param>
    /// <param name="eventSourceId">Optional <see cref="EventSourceId"/> to filter for.</param>
    /// <param name="eventTypes">Optional collection of <see cref="EventType">event types</see> to filter for.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns><see cref="IEventCursor"/>.</returns>
    Task<IEventCursor> GetRange(EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = default, IEnumerable<EventType>? eventTypes = default, CancellationToken cancellationToken = default);
}
