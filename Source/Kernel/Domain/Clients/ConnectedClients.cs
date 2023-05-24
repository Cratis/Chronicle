// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Domain.Clients;

/// <summary>
/// Represents the endpoint for clients to connect to.
/// </summary>
[Route("/api/clients/{microserviceId}")]
public class ConnectedClients : Controller
{
    readonly ILogger<ConnectedClients> _logger;
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for working with Orleans grains.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(
        IGrainFactory grainFactory,
        ILogger<ConnectedClients> logger)
    {
        _logger = logger;
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// A ping endpoint for clients to see if Kernel is available.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> that is connecting.</param>
    /// <param name="connectionId">The unique identifier of the connection that is pinging.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ConnectionNotFoundForClient">Thrown if the connection is disconnected.</exception>
    [HttpPost("ping/{connectionId}")]
    public async Task Ping(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId)
    {
        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
        if (!await connectedClients.OnClientPing(connectionId))
        {
            throw new ConnectionNotFoundForClient(connectionId);
        }
    }

    /// <summary>
    /// Connect a client.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> that is connecting.</param>
    /// <param name="connectionId">The unique identifier of the connection.</param>
    /// <param name="clientInformation"><see cref="ClientInformation"/> to connect with.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("connect/{connectionId}")]
    public async Task Connect(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId,
        [FromBody] ClientInformation clientInformation)
    {
        var uri = new Uri(clientInformation.AdvertisedUri);

        if (microserviceId != MicroserviceId.Kernel)
        {
            uri = uri.AdjustForDockerHost();
        }

        _logger.ClientConnected(clientInformation.ClientVersion, microserviceId, connectionId, uri);
        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
        await connectedClients.OnClientConnected(
            connectionId,
            uri,
            clientInformation.ClientVersion,
            clientInformation.IsRunningWithDebugger);
    }

    /// <summary>
    /// Disconnect a client.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> that is connecting.</param>
    /// <param name="connectionId">The unique identifier of the connection.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("disconnect/{connectionId}")]
    public async Task Disconnect(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId)
    {
        _logger.ClientDisconnected(microserviceId, connectionId);
        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
        await connectedClients.OnClientDisconnected(
            connectionId,
            "Explicit disconnect");
    }
}
