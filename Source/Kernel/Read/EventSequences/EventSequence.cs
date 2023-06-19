// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Aksio.Cratis.Kernel.Read.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}")]
public class EventSequence : Controller
{
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProviderProvider;
    readonly ProviderFor<IObserverStorage> _observerStorage;
    readonly IGrainFactory _grainFactory;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProviderProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="observerStorage">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public EventSequence(
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProviderProvider,
        ProviderFor<IObserverStorage> observerStorage,
        IGrainFactory grainFactory,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager)
    {
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
        _observerStorage = observerStorage;
        _grainFactory = grainFactory;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Get the head sequence number.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <returns>The tail sequence number.</returns>
    [HttpGet("next-sequence-number")]
    public Task<EventSequenceNumber> Next(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId) =>
        GetEventSequence(microserviceId, eventSequenceId, tenantId).GetNextSequenceNumber();

    /// <summary>
    /// Get the tail sequence number.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <returns>The tail sequence number.</returns>
    [HttpGet("tail-sequence-number")]
    public Task<EventSequenceNumber> Tail(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId) =>
        GetEventSequence(microserviceId, eventSequenceId, tenantId).GetTailSequenceNumber();

    /// <summary>
    /// Get the tail sequence number for a specific observer.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <param name="observerId">The observer to get for.</param>
    /// <returns>The tail sequence number.</returns>
    /// <remarks>
    /// This will use the event types of the observer to determine the tail sequence number of
    /// the last event that was appended to the event sequence that the observer is interested in.
    /// </remarks>
    [HttpGet("tail-sequence-number/observer/{observerId}")]
    public async Task<EventSequenceNumber> TailForObserver(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId)
    {
        var correlationId = _executionContextManager.Current.CorrelationId;
        _executionContextManager.Establish(tenantId, correlationId, microserviceId);

        var observer = await _observerStorage().GetObserver(observerId);
        return await _eventSequenceStorageProviderProvider().GetTailSequenceNumber(eventSequenceId, observer.EventTypes);
    }

    /// <summary>
    /// Get events for a specific event sequence in a microservice for a specific tenant.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <param name="pageSize">Size of page to return.</param>
    /// <param name="pageNumber">Page number to return.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<PagedQueryResult<AppendedEventWithJsonAsContent>> GetAppendedEvents(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromQuery] int pageSize = 100,
        [FromQuery] int pageNumber = 0)
    {
        var result = new List<AppendedEventWithJsonAsContent>();

        var correlationId = _executionContextManager.Current.CorrelationId;
        _executionContextManager.Establish(tenantId, correlationId, microserviceId);

        var from = EventSequenceNumber.First + (pageNumber * pageSize);
        var tail = await _eventSequenceStorageProviderProvider().GetTailSequenceNumber(eventSequenceId);
        var cursor = await _eventSequenceStorageProviderProvider().GetRange(eventSequenceId, from, from + (pageSize - 1));
        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current.Select(_ => new AppendedEventWithJsonAsContent(
                _.Metadata,
                _.Context,
                JsonSerializer.SerializeToNode(_.Content, _jsonSerializerOptions)!)));
        }
        return new(result, tail);
    }

    /// <summary>
    /// Get a histogram of a specific event sequence. PS: Not implemented yet.
    /// </summary>
    /// <returns>A collection of <see cref="EventHistogramEntry"/>.</returns>
    [HttpGet("histogram")]
    public Task<IEnumerable<EventHistogramEntry>> Histogram(/*[FromRoute] EventSequenceId eventSequenceId*/) => Task.FromResult(Array.Empty<EventHistogramEntry>().AsEnumerable());

    IEventSequence GetEventSequence(MicroserviceId microserviceId, EventSequenceId eventSequenceId, TenantId tenantId) =>
        _grainFactory.GetGrain<IEventSequence>(eventSequenceId, keyExtension: new MicroserviceAndTenant(microserviceId, tenantId));
}
