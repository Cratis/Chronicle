// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Client;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Grains.Workers;
using Microsoft.AspNetCore.Mvc;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;
using EventRedacted = Aksio.Cratis.Kernel.Events.EventSequences.EventRedacted;
using Aksio.Cratis.Kernel.Events.EventSequences;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}")]
public class EventSequence : Controller
{
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly IClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    /// <param name="causationManager">The <see cref="ICausationManager"/> for working with causation.</param>
    /// <param name="identityProvider">The <see cref="IIdentityProvider"/> for getting current identity.</param>
    /// <param name="client"><see cref="IClient"/> for working with Cratis artifacts.</param>
    public EventSequence(
        IGrainFactory grainFactory,
        IExecutionContextManager executionContextManager,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IClient client)
    {
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _client = client;
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
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);
        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
        await eventSequence.Append(
            eventToAppend.EventSourceId,
            eventToAppend.EventType,
            eventToAppend.Content,
            eventToAppend.Causation,
            eventToAppend.CausedBy,
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
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);
        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
        var events = eventsToAppend.Events.Select(_ => new Grains.EventSequences.EventToAppend(eventsToAppend.EventSourceId, _.EventType, _.Content, _.ValidFrom)).ToArray();
        await eventSequence.AppendMany(events, eventsToAppend.Causation, eventsToAppend.CausedBy);
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
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, MicroserviceId.Kernel);
        var sequence = _client.GetEventSequences(TenantId.NotSet).GetEventSequence(EventSequenceId.SystemId);
        await sequence.Append(
            EventSequenceId.Log.Value,
            new EventRedacted(
                microserviceId,
                tenantId,
                EventSequenceId.Log,
                redaction.SequenceNumber,
                redaction.Reason));
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

        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, MicroserviceId.Kernel);
        var sequence = _client.GetEventSequences(TenantId.NotSet).GetEventSequence(EventSequenceId.SystemId);
        await sequence.Append(
            EventSequenceId.Log.Value,
            new EventsRedactedForEventSource(
                microserviceId,
                tenantId,
                EventSequenceId.Log,
                redaction.EventSourceId,
                redaction.EventTypes.Select(_ => new EventType(_, EventGeneration.Unspecified)).ToArray(),
                redaction.Reason));
    }

    IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        _grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
}
