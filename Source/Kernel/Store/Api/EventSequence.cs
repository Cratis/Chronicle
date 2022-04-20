// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Store.Grains;
using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Events.Store.Api;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
[Route("/api/events/store/sequence")]
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

    [HttpPost("{eventSourceId}/{eventTypeId}/{eventGeneration}")]
    public async Task Append(
        [FromRoute] EventSourceId eventSourceId,
        [FromRoute] EventTypeId eventTypeId,
        [FromRoute] EventGeneration eventGeneration)
    {
        var jsonDocument = await JsonDocument.ParseAsync(Request.Body);
        var content = JsonObject.Create(jsonDocument.RootElement);
        var eventLog = _grainFactory.GetGrain<IEventSequence>(EventSequenceId.Log, keyExtension: _executionContextManager.Current.ToMicroserviceAndTenant());
        await eventLog.Append(
            eventSourceId,
            new EventType(eventTypeId, eventGeneration),
            content!);
    }

    [HttpGet("{eventSequenceId}")]
    public async Task<IEnumerable<AppendedEvent>> FindFor(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] MicroserviceId microserviceId,
        [FromQuery] TenantId tenantId)
    {
        var result = new List<AppendedEvent>();
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var cursor = await _eventSequenceStorageProviderProvider().GetFromSequenceNumber(EventSequenceNumber.First);
        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current);
        }
        return result;
    }

    [HttpGet("{eventSequenceId}/count")]
    public Task<long> Count() => Task.FromResult(0L);

    [HttpGet("histogram")]
    public Task<IEnumerable<EventHistogramEntry>> Histogram([FromRoute] string eventLogId) => Task.FromResult(Array.Empty<EventHistogramEntry>().AsEnumerable());

    [HttpGet("{eventSequenceId}/{eventSourceId}")]
    public Task FindForEventSourceId(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] EventSourceId eventSourceId)
    {
        return Task.CompletedTask;
    }

    [HttpGet("{eventSequenceId}/types")]
    public Task Types([FromRoute] string eventSequenceId)
    {
        return Task.CompletedTask;
    }
}
