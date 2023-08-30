// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;
using Aksio.Cratis.Observation.Reducers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/reducers")]
public class Reducers : Controller
{
    readonly KernelConfiguration _configuration;
    readonly IGrainFactory _grainFactory;
    readonly ILogger<Reducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="configuration">The Kernel configuration.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Reducers(
        KernelConfiguration configuration,
        IGrainFactory grainFactory,
        ILogger<Reducers> logger)
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
    /// <param name="definitions">Collection of <see cref="ReducerDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("register/{connectionId}")]
    public async Task Register(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId,
        [FromBody] IEnumerable<ReducerDefinition> definitions)
    {
        _logger.RegisterReducers();

        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
        var client = await connectedClients.GetConnectedClient(connectionId);
        var tenants = client.IsMultiTenanted ? _configuration.Tenants.GetTenantIds() : new TenantId[] { TenantId.NotSet };

        var reducers = _grainFactory.GetGrain<IClientReducers>(microserviceId);
        await reducers.Register(connectionId, definitions, tenants);
    }
}
