// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Connections;
using Cratis.Kernel.Configuration;
using Cratis.Kernel.Grains.Clients;
using Cratis.Kernel.Grains.Observation.Reducers.Clients;
using Cratis.Observation.Reducers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="configuration">The Kernel configuration.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Route("/api/events/store/{microserviceId}/reducers")]
public class Reducers(
    KernelConfiguration configuration,
    IGrainFactory grainFactory,
    ILogger<Reducers> logger) : ControllerBase
{
    /// <summary>
    /// Register client observers for a specific microservice and unique connection.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to register for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to register with.</param>
    /// <param name="definitions">Collection of <see cref="ReducerDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("register/{connectionId}")]
    public Task Register(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId,
        [FromBody] IEnumerable<ReducerDefinition> definitions)
    {
        logger.RegisterReducers();

        _ = Task.Run(async () =>
        {
            var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
            var client = await connectedClients.GetConnectedClient(connectionId);
            var tenants = configuration.Tenants.GetTenantIds();

            var reducers = grainFactory.GetGrain<IClientReducers>(microserviceId);
            await reducers.Register(connectionId, definitions, tenants);
        });

        return Task.CompletedTask;
    }
}
