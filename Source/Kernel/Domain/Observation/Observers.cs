// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using IClientObservers = Aksio.Cratis.Kernel.Grains.Observation.Clients.IClientObservers;

namespace Aksio.Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/observers")]
public class Observers : Controller
{
    readonly KernelConfiguration _configuration;
    readonly IGrainFactory _grainFactory;
    readonly ILogger<Observers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="configuration">The Kernel configuration.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Observers(
        KernelConfiguration configuration,
        IGrainFactory grainFactory,
        ILogger<Observers> logger)
    {
        _configuration = configuration;
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
        _ = Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();

            var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
            var client = await connectedClients.GetConnectedClient(connectionId);
            var tenants = client.IsMultiTenanted ? _configuration.Tenants.GetTenantIds() : new TenantId[] { TenantId.NotSet };

            var observers = _grainFactory.GetGrain<IClientObservers>(microserviceId);
            await observers.Register(connectionId, registrations, tenants);

            stopwatch.Stop();
            _logger.ObserversRegistered(registrations.Count(), microserviceId, stopwatch.Elapsed);
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
    /// Retry a specific partition for a microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <param name="partitionId"><see cref="EventSourceId">Partition</see> to retry.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}/failed-partitions/{tenantId}/retry/{partitionId}")]
    public async Task RetryPartition(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId,
        [FromRoute] EventSourceId partitionId)
    {
        var observer = _grainFactory.GetGrain<IObserverSupervisor>(observerId, new ObserverKey(microserviceId, tenantId, EventSequenceId.Log));
        await observer.TryResumePartition(partitionId);
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
