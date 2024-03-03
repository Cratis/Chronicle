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
[Route("/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}")]
public class EventSequence : ControllerBase
{
    readonly IGrainFactory _grainFactory;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IEventSerializer _eventSerializer;
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
    /// <param name="causationManager">The <see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="identityProvider">The <see cref="IIdentityProvider"/> for getting current identity.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
    public EventSequence(
        IGrainFactory grainFactory,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IEventSerializer eventSerializer,
        IEventTypes eventTypes)
    {
        _grainFactory = grainFactory;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _eventSerializer = eventSerializer;
        _eventTypes = eventTypes;
    }

    /// <summary>
    /// Appends an event to the event log.
    /// </summary>
    /// <param name="microserviceId">The microservice to append for.</param>
    /// <param name="eventSequenceId">The event sequence to append to.</param>
    /// <param name="tenantId">The tenant to append to.</param>
    /// <param name="eventToAppend">The payload with the details about the event to append.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task Append(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] TenantId tenantId,
        [FromBody] AppendEvent eventToAppend)
    {
        var causation = eventToAppend.Causation ?? _causationManager.GetCurrentChain();
        var causedBy = eventToAppend.CausedBy ?? _identityProvider.GetCurrent();

        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
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
    /// <param name="microserviceId">The microservice to append for.</param>
    /// <param name="eventSequenceId">The event sequence to append to.</param>
    /// <param name="tenantId">The tenant to append to.</param>
    /// <param name="eventsToAppend">The payload with the details about the events to append.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("append-many")]
    public async Task AppendMany(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] TenantId tenantId,
        [FromBody] AppendManyEvents eventsToAppend)
    {
        var causation = eventsToAppend.Causation ?? _causationManager.GetCurrentChain();
        var causedBy = eventsToAppend.CausedBy ?? _identityProvider.GetCurrent();

        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
        var events = eventsToAppend.Events.Select(_ => new Grains.EventSequences.EventToAppend(eventsToAppend.EventSourceId, _.EventType, _.Content, _.ValidFrom)).ToArray();
        await eventSequence.AppendMany(events, causation, causedBy);
    }

    /// <summary>
    /// Redact a specific single event by its sequence number.
    /// </summary>
    /// <param name="microserviceId">The microservice to redact for.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="tenantId">The tenant to redact for.</param>
    /// <param name="redaction">The <see cref="RedactEvent"/> to redact.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-event")]
    public async Task RedactEvent(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] TenantId tenantId,
        [FromBody] RedactEvent redaction)
    {
        var causation = redaction.Causation ?? _causationManager.GetCurrentChain();
        var causedBy = redaction.CausedBy ?? _identityProvider.GetCurrent();

        var eventType = _eventTypes.GetEventTypeFor(typeof(EventRedacted));
        var @event = new EventRedacted(
                microserviceId,
                tenantId,
                EventSequenceId.Log,
                redaction.SequenceNumber,
                redaction.Reason);
        var content = await _eventSerializer.Serialize(@event);

        var eventSequence = GetEventSequence(MicroserviceId.Kernel, EventSequenceId.System, TenantId.NotSet);
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
    /// <param name="microserviceId">The microservice to redact for.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="tenantId">The tenant to redact for.</param>
    /// <param name="redaction">The redaction filter to use.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-events")]
    public async Task RedactEvents(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] TenantId tenantId,
        [FromBody] RedactEvents redaction)
    {
        var causation = redaction.Causation ?? _causationManager.GetCurrentChain();
        var causedBy = redaction.CausedBy ?? _identityProvider.GetCurrent();

        var eventType = _eventTypes.GetEventTypeFor(typeof(EventsRedactedForEventSource));
        var @event = new EventsRedactedForEventSource(
                microserviceId,
                tenantId,
                EventSequenceId.Log,
                redaction.EventSourceId,
                redaction.EventTypes.Select(_ => new EventType(_, EventGeneration.Unspecified)).ToArray(),
                redaction.Reason);
        var content = await _eventSerializer.Serialize(@event);

        var eventSequence = GetEventSequence(MicroserviceId.Kernel, EventSequenceId.System, TenantId.NotSet);
        await eventSequence.Append(
            EventSequenceId.Log.Value,
            eventType,
            content,
            causation,
            causedBy);
    }

    IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        _grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new EventSequenceKey(microserviceId, tenantId));
}
