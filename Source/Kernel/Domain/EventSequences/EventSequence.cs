// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        var eventLog = _grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
        await eventLog.Append(
            eventToAppend.EventSourceId,
            eventToAppend.EventType,
            eventToAppend.Content);
    }
}
