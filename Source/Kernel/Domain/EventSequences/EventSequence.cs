// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public EventSequence(
        IGrainFactory grainFactory,
        IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
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
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
        await eventSequence.Append(
            eventToAppend.EventSourceId,
            eventToAppend.EventType,
            eventToAppend.Content);
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
    public async Task Redact(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] TenantId tenantId,
        [FromBody] RedactEvent redaction)
    {
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
        await eventSequence.Redact(redaction.SequenceNumber, redaction.Reason);
    }

    /// <summary>
    /// Redact multiple events
    /// </summary>
    /// <param name="microserviceId">The microservice to redact for.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="tenantId">The tenant to redact for.</param>
    /// <param name="redaction">The redaction filter to use.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-events")]
    public async Task Redact(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] TenantId tenantId,
        [FromBody] RedactEvents redaction)
    {
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var eventSequence = GetEventSequence(microserviceId, eventSequenceId, tenantId);
        await eventSequence.Redact(
            redaction.EventSourceId,
            redaction.Reason,
            redaction.EventTypes.Select(_ => new EventType(_, EventGeneration.Unspecified)).ToArray());
    }

    IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        _grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
}
