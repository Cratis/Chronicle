// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Defines the event sequence.
/// </summary>
public interface IEventSequence : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Rehydrate the event sequence.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rehydrate();

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
    /// Get the sequence number of the tail event in the sequence filtered by event types.
    /// </summary>
    /// <param name="eventTypes">Event types to filter on.</param>
    /// <returns>Tail sequence number.</returns>
    /// <remarks>
    /// The method will filter down on the event types and give you the highest sequence number for the given event types.
    /// </remarks>
    Task<EventSequenceNumber> GetTailSequenceNumberForEventTypes(IEnumerable<EventType> eventTypes);

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
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the event, defined by <see cref="Identity"/>.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSourceId eventSourceId, EventType eventType, JsonObject content, IEnumerable<Causation> causation, Identity causedBy, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="events">Collection of <see cref="EventToAppend">events</see> to append.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the events, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task AppendMany(IEnumerable<EventToAppend> events, IEnumerable<Causation> causation, Identity causedBy);

    /// <summary>
    /// Compensate a specific event in the event store.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event to compensate.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to compensate.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the compensation, defined by <see cref="Identity"/>.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// The type of the event has to be the same as the original event at the sequence number.
    /// Its generational information is taken into account when compensating.
    /// </remarks>
    Task Compensate(EventSequenceNumber sequenceNumber, EventType eventType, JsonObject content, IEnumerable<Causation> causation, Identity causedBy, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the redaction, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason, IEnumerable<Causation> causation, Identity causedBy);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the redaction, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType> eventTypes, IEnumerable<Causation> causation, Identity causedBy);
}
