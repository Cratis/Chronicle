// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Reducers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/reducers")]
public class Reducers : Controller
{
    readonly IGrainFactory _grainFactory;
    readonly ILogger<Reducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Reducers(
        IGrainFactory grainFactory,
        ILogger<Reducers> logger)
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
        [FromBody] IEnumerable<ClientReducersRegistration> registrations)
    {
        _logger.RegisterReducers();

        _ = Task.Run(() =>
        {
            // var observers = _grainFactory.GetGrain<IClientObservers>(microserviceId);
            // return observers.Register(connectionId, registrations);
        });

        return Task.CompletedTask;
    }
}
