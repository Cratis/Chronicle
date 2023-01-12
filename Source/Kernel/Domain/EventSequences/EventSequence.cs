// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Microsoft.AspNetCore.Mvc;
using Orleans;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}")]
public class EventSequence : Controller
{
    readonly IGrainFactory _grainFactory;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProviderProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
    /// <param name="eventSequenceStorageProviderProvider">Provider for <see cref="IEventSequenceStorageProvider"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public EventSequence(
        IGrainFactory grainFactory,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProviderProvider,
        IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
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

    /// <summary>
    /// Get events for a specific event sequence in a microservice for a specific tenant.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<AppendedEvent>> FindFor(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] MicroserviceId microserviceId,
        [FromQuery] TenantId tenantId)
    {
        var result = new List<AppendedEvent>();
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var cursor = await _eventSequenceStorageProviderProvider().GetFromSequenceNumber(eventSequenceId, EventSequenceNumber.First);
        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current);
        }
        return result;
    }
}
