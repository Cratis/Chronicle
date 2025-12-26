// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Monads;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Defines the event sequence.
/// </summary>
public interface IEventSequence : IGrainWithStringKey
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
    [AlwaysInterleave]
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
    [AlwaysInterleave]
    Task<Result<EventSequenceNumber, GetSequenceNumberError>> GetNextSequenceNumberGreaterOrEqualTo(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null);

    /// <summary>
    /// Appends an event to the event sequence.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/> for the event.</param>
    /// <param name="causedBy">The <see cref="Identity"/> of the entity that caused the event.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to specify the type of the event source. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to specify the type of the event stream. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to specify the identifier of the event stream. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <returns>An <see cref="AppendResult"/> indicating the result of the append operation.</returns>
    Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        CorrelationId? correlationId = default,
        IEnumerable<Causation>? causation = default,
        Identity? causedBy = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> to append for.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="eventStreamType">the <see cref="EventStreamType"/> to append to.</param>
    /// <param name="eventStreamId">The <see cref="EventStreamId"/> to append to.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the event, defined by <see cref="Identity"/>.</param>
    /// <param name="concurrencyScope">The <see cref="ConcurrencyScope"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task<AppendResult> Append(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScope concurrencyScope);

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="events">Collection of <see cref="EventToAppend">events</see> to append.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the events, defined by <see cref="Identity"/>.</param>
    /// <param name="concurrencyScopes">The <see cref="ConcurrencyScopes"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task<AppendManyResult> AppendMany(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopes concurrencyScopes);

    /// <summary>
    /// Compensate a specific event in the event store.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event to compensate.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to compensate.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the compensation, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// The type of the event has to be the same as the original event at the sequence number.
    /// Its generational information is taken into account when compensating.
    /// </remarks>
    Task Compensate(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the redaction, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/>.</param>
    /// <param name="causedBy">The person, system or service that caused the redaction, defined by <see cref="Identity"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType> eventTypes,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy);
}
