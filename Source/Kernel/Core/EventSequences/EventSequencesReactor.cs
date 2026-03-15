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
/// Represents a reactor that handles event sequence system events such as redaction and revision,
/// performing the actual storage operations in response to events in the System event sequence.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for deserializing event content.</param>
/// <param name="logger">The <see cref="ILogger{EventSequencesReactor}"/> for logging.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: false, defaultNamespaceOnly: false)]
public class EventSequencesReactor(IGrainFactory grainFactory, JsonSerializerOptions jsonSerializerOptions, ILogger<EventSequencesReactor> logger) : Reactor
{
    /// <summary>
    /// Performs the actual redaction of a single event when an <see cref="EventRedactionRequested"/> system event is observed.
    /// The audit context (who triggered it, when, which correlation) is carried in the <paramref name="context"/>.
    /// </summary>
    /// <param name="event">The system event containing the target sequence and reason.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event, which holds the redaction audit context.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RedactionRequested(EventRedactionRequested @event, EventContext context)
    {
        logger.Redacting(context.EventStore, context.Namespace, @event.Sequence, @event.SequenceNumber);

        var eventSequence = grainFactory.GetEventSequence(@event.Sequence, context.EventStore, context.Namespace);
        await eventSequence.Redact(
            @event.SequenceNumber,
            @event.Reason,
            context.CorrelationId,
            context.Causation,
            context.CausedBy);
    }

    /// <summary>
    /// Performs the actual redaction of all events for an event source when an <see cref="EventsRedactedForEventSource"/> system event is observed.
    /// The audit context (who triggered it, when, which correlation) is carried in the <paramref name="context"/>.
    /// </summary>
    /// <param name="event">The system event containing the target event source and reason.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event, which holds the redaction audit context.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RedactedForEventSource(EventsRedactedForEventSource @event, EventContext context)
    {
        logger.RedactingForEventSource(context.EventStore, context.Namespace, @event.Sequence, @event.EventSourceId, @event.EventTypes);

        var eventSequence = grainFactory.GetEventSequence(@event.Sequence, context.EventStore, context.Namespace);
        await eventSequence.Redact(
            @event.EventSourceId,
            @event.Reason,
            @event.EventTypes,
            context.CorrelationId,
            context.Causation,
            context.CausedBy);
    }

    /// <summary>
    /// Performs the actual revision of an event when an <see cref="EventRevised"/> system event is observed.
    /// The audit context (who triggered it, when, which correlation) is carried in the <paramref name="context"/>.
    /// </summary>
    /// <param name="event">The system event containing the target sequence, event type and new content.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event, which holds the revision audit context.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the revision content is null or contains invalid JSON.</exception>
    public async Task Revised(EventRevised @event, EventContext context)
    {
        logger.Revising(context.EventStore, context.Namespace, @event.Sequence, @event.SequenceNumber, @event.EventType);

        var eventSequence = grainFactory.GetEventSequence(@event.Sequence, context.EventStore, context.Namespace);
        var content = (JsonSerializer.Deserialize<JsonNode>(@event.Content, jsonSerializerOptions)
            ?? throw new InvalidOperationException($"Revision content for event at sequence {(ulong)@event.SequenceNumber} is null or invalid JSON."))
            .AsObject();
        await eventSequence.Revise(
            @event.SequenceNumber,
            @event.EventType,
            content,
            context.CorrelationId,
            context.Causation,
            context.CausedBy);
    }
}
