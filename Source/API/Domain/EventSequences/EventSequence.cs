// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Auditing;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Identities;
using Cratis.Kernel.Events.EventSequences;
using Cratis.Kernel.Storage.EventSequences;
using Microsoft.AspNetCore.Mvc;
using EventRedacted = Cratis.Kernel.Events.EventSequences.EventRedacted;
using IEventSequence = Cratis.Kernel.Grains.EventSequences.IEventSequence;

#pragma warning disable SA1600, IDE0060

namespace Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequence"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
/// <param name="causationManager">The <see cref="ICausationManager"/> for working with causation.</param>
/// <param name="identityProvider">The <see cref="IIdentityProvider"/> for getting current identity.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
[Route("/api/events/store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequence(
    IGrainFactory grainFactory,
    ICausationManager causationManager,
    IIdentityProvider identityProvider,
    IEventSerializer eventSerializer,
    IEventTypes eventTypes) : ControllerBase
{
    /// <summary>
    /// Appends an event to the event log.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to append to.</param>
    /// <param name="eventToAppend">The payload with the details about the event to append.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task Append(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromBody] AppendEvent eventToAppend)
    {
        var causation = eventToAppend.Causation ?? causationManager.GetCurrentChain();
        var causedBy = eventToAppend.CausedBy ?? identityProvider.GetCurrent();

        var eventSequence = GetEventSequence(eventStore, @namespace, eventSequenceId);
        await eventSequence.Append(
            eventToAppend.EventSourceId,
            eventToAppend.EventType,
            eventToAppend.Content,
            causation,
            causedBy,
            eventToAppend.ValidFrom);
    }

    /// <summary>
    /// Appends an event to the event log.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to append to.</param>
    /// <param name="eventsToAppend">The payload with the details about the events to append.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("append-many")]
    public async Task AppendMany(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromBody] AppendManyEvents eventsToAppend)
    {
        var causation = eventsToAppend.Causation ?? causationManager.GetCurrentChain();
        var causedBy = eventsToAppend.CausedBy ?? identityProvider.GetCurrent();

        var eventSequence = GetEventSequence(eventStore, @namespace, eventSequenceId);
        var events = eventsToAppend.Events.Select(_ => new Grains.EventSequences.EventToAppend(eventsToAppend.EventSourceId, _.EventType, _.Content, _.ValidFrom)).ToArray();
        await eventSequence.AppendMany(events, causation, causedBy);
    }

    /// <summary>
    /// Redact a specific single event by its sequence number.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="redaction">The <see cref="RedactEvent"/> to redact.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-event")]
    public async Task RedactEvent(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromBody] RedactEvent redaction)
    {
        var causation = redaction.Causation ?? causationManager.GetCurrentChain();
        var causedBy = redaction.CausedBy ?? identityProvider.GetCurrent();

        var eventType = eventTypes.GetEventTypeFor(typeof(EventRedacted));
        var @event = new EventRedacted(
                eventSequenceId,
                redaction.SequenceNumber,
                redaction.Reason);
        var content = await eventSerializer.Serialize(@event);

        var eventSequence = GetEventSequence(eventStore, @namespace, EventSequenceId.System);
        await eventSequence.Append(
            EventSequenceId.Log.Value,
            eventType,
            content,
            causation,
            causedBy);
    }

    /// <summary>
    /// Redact multiple events.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="redaction">The redaction filter to use.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-events")]
    public async Task RedactEvents(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromBody] RedactEvents redaction)
    {
        var causation = redaction.Causation ?? causationManager.GetCurrentChain();
        var causedBy = redaction.CausedBy ?? identityProvider.GetCurrent();

        var eventType = eventTypes.GetEventTypeFor(typeof(EventsRedactedForEventSource));
        var @event = new EventsRedactedForEventSource(
                eventSequenceId,
                redaction.EventSourceId,
                redaction.EventTypes.Select(_ => new EventType(_, EventGeneration.Unspecified)).ToArray(),
                redaction.Reason);
        var content = await eventSerializer.Serialize(@event);

        var eventSequence = GetEventSequence(eventStore, @namespace, EventSequenceId.System);
        await eventSequence.Append(
            EventSequenceId.Log.Value,
            eventType,
            content,
            causation,
            causedBy);
    }

    IEventSequence GetEventSequence(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new EventSequenceKey(eventStore, @namespace));
}
