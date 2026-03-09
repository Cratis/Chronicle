// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a reactor that handles event sequence system events such as redaction and compensation,
/// performing the actual storage operations in response to events in the System event sequence.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for deserializing event content.</param>
/// <param name="logger">The <see cref="ILogger{EventSequencesReactor}"/> for logging.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: false, defaultNamespaceOnly: false)]
public class EventSequencesReactor(IGrainFactory grainFactory, JsonSerializerOptions jsonSerializerOptions, ILogger<EventSequencesReactor> logger) : Reactor
{
    /// <summary>
    /// Performs the actual redaction of a single event when an <see cref="EventRedacted"/> system event is observed.
    /// </summary>
    /// <param name="event">The system event containing redaction details.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Redacted(EventRedacted @event, EventContext context)
    {
        logger.Redacting(context.EventStore, context.Namespace, @event.Sequence, @event.SequenceNumber);

        var eventSequence = grainFactory.GetEventSequence(@event.Sequence, context.EventStore, context.Namespace);
        await eventSequence.Redact(
            @event.SequenceNumber,
            @event.Reason,
            @event.CorrelationId,
            @event.Causation,
            @event.CausedBy);
    }

    /// <summary>
    /// Performs the actual redaction of all events for an event source when an <see cref="EventsRedactedForEventSource"/> system event is observed.
    /// </summary>
    /// <param name="event">The system event containing redaction details.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RedactedForEventSource(EventsRedactedForEventSource @event, EventContext context)
    {
        logger.RedactingForEventSource(context.EventStore, context.Namespace, @event.Sequence, @event.EventSourceId, @event.EventTypes);

        var eventSequence = grainFactory.GetEventSequence(@event.Sequence, context.EventStore, context.Namespace);
        await eventSequence.Redact(
            @event.EventSourceId,
            @event.Reason,
            @event.EventTypes,
            @event.CorrelationId,
            @event.Causation,
            @event.CausedBy);
    }

    /// <summary>
    /// Performs the actual compensation of an event when an <see cref="EventCompensated"/> system event is observed.
    /// </summary>
    /// <param name="event">The system event containing compensation details.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the compensation content is null or contains invalid JSON.</exception>
    public async Task Compensated(EventCompensated @event, EventContext context)
    {
        logger.Compensating(context.EventStore, context.Namespace, @event.Sequence, @event.SequenceNumber, @event.EventType);

        var eventSequence = grainFactory.GetEventSequence(@event.Sequence, context.EventStore, context.Namespace);
        var content = (JsonSerializer.Deserialize<JsonNode>(@event.Content, jsonSerializerOptions)
            ?? throw new InvalidOperationException($"Compensation content for event at sequence {(ulong)@event.SequenceNumber} is null or invalid JSON."))
            .AsObject();
        await eventSequence.Compensate(
            @event.SequenceNumber,
            @event.EventType,
            content,
            @event.CorrelationId,
            @event.Causation,
            @event.CausedBy);
    }
}
