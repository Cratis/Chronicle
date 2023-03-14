// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/sequence/{eventSequenceId}")]
public class EventSequence : Controller
{
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProviderProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProviderProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public EventSequence(
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProviderProvider,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager)
    {
        _eventSequenceStorageProviderProvider = eventSequenceStorageProviderProvider;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Get events for a specific event sequence in a microservice for a specific tenant.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to get for.</param>
    /// <param name="microserviceId">Microservice to get for.</param>
    /// <param name="tenantId">Tenant to get for.</param>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<AppendedEventWithJsonAsContent>> FindFor(
        [FromRoute] EventSequenceId eventSequenceId,
        [FromQuery] MicroserviceId microserviceId,
        [FromQuery] TenantId tenantId)
    {
        var result = new List<AppendedEventWithJsonAsContent>();
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);
        var cursor = await _eventSequenceStorageProviderProvider().GetFromSequenceNumber(eventSequenceId, EventSequenceNumber.First);
        while (await cursor.MoveNext())
        {
            result.AddRange(cursor.Current.Select(_ => new AppendedEventWithJsonAsContent(
                _.Metadata,
                _.Context,
                JsonSerializer.SerializeToNode(_.Content, _jsonSerializerOptions)!)));
        }
        return result;
    }

    /// <summary>
    /// Get a histogram of a specific event sequence. PS: Not implemented yet.
    /// </summary>
    /// <returns>A collection of <see cref="EventHistogramEntry"/>.</returns>
    [HttpGet("histogram")]
    public Task<IEnumerable<EventHistogramEntry>> Histogram(/*[FromRoute] EventSequenceId eventSequenceId*/) => Task.FromResult(Array.Empty<EventHistogramEntry>().AsEnumerable());
}
