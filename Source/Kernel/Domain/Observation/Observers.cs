// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/observers")]
public class Observers : Controller
{
    readonly IGrainFactory _grainFactory;
    readonly ILogger<Observers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Observers(
        IGrainFactory grainFactory,
        ILogger<Observers> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    /// <summary>
    /// Register client observers for a specific microservice and unique connection.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to register for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to register with.</param>
    /// <param name="registrations">Collection of <see cref="ClientObserverRegistration"/>.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("register/{connectionId}")]
    public Task Register(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId,
        [FromBody] IEnumerable<ClientObserverRegistration> registrations)
    {
        _logger.RegisterObservers();

        _ = Task.Run(() =>
        {
            var observers = _grainFactory.GetGrain<IClientObservers>(microserviceId);
            return observers.Register(connectionId, registrations);
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Rewind a specific observer for a microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}/rewind/{tenantId}")]
    public async Task Rewind(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId)
    {
        var observer = _grainFactory.GetGrain<IObserverSupervisor>(observerId, new ObserverKey(microserviceId, tenantId, EventSequenceId.Log));
        await observer.Rewind();
    }

    /// <summary>
    /// Rewind a specific observer for a microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <param name="eventSourceId">Specific <see cref="EventSourceId"/> to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}/rewind/{tenantId}/{eventSourceId}")]
    public async Task RewindPartition(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId,
        [FromRoute] EventSourceId eventSourceId)
    {
        var observer = _grainFactory.GetGrain<IObserverSupervisor>(observerId, new ObserverKey(microserviceId, tenantId, EventSequenceId.Log));
        await observer.RewindPartition(eventSourceId);
    }
}
