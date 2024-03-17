// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Connections;
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
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Route("/api/events/store/{eventStore}/reducers")]
public class Reducers(
    IGrainFactory grainFactory,
    ILogger<Reducers> logger) : ControllerBase
{
    /// <summary>
    /// Register client observers for a specific event store and unique connection.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to register for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to register with.</param>
    /// <param name="definitions">Collection of <see cref="ReducerDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("register/{connectionId}")]
    public Task Register(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] ConnectionId connectionId,
        [FromBody] IEnumerable<ReducerDefinition> definitions)
    {
        logger.RegisterReducers();

        _ = Task.Run(async () =>
        {
            var connectedClients = grainFactory.GetGrain<IConnectedClients>(0);
            var client = await connectedClients.GetConnectedClient(connectionId);
            var reducers = grainFactory.GetGrain<IClientReducers>(eventStore);

            // TODO: This needs to register all reducers for all namespaces - or rather, the client reducers internally should deal with that!
            await reducers.Register(connectionId, definitions, Enumerable.Empty<EventStoreNamespaceName>());
        });

        return Task.CompletedTask;
    }
}
