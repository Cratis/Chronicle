// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Clients;
using Microsoft.AspNetCore.Http;
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
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for working with Orleans grains.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for deserialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(
        IGrainFactory grainFactory,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<ConnectedClients> logger)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// A ping endpoint for clients to see if Kernel is available.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> that is connecting.</param>
    /// <param name="connectionId">The unique identifier of the connection that is pinging.</param>
    /// <returns>Awaitable task.</returns>
    [HttpGet("ping/{connectionId}")]
    public async Task Ping(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId)
    {
        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
        await connectedClients.OnClientPing(connectionId);
    }

    /// <summary>
    /// Accepts client connections over Web Sockets.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> that is connecting.</param>
    /// <param name="connectionId">The unique identifier of the connection.</param>
    /// <param name="clientInformation"><see cref="ClientInformation"/> to connect with.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task Connect(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] ConnectionId connectionId,
        [FromBody] ClientInformation clientInformation)
    {
        _logger.ClientConnected(clientInformation.ClientVersion, microserviceId, connectionId);

        var connectedClients = _grainFactory.GetGrain<IConnectedClients>(microserviceId);
        await connectedClients.OnClientConnected(
            connectionId,
            new Uri(clientInformation.AdvertisedUri),
            clientInformation.ClientVersion);



        //     await webSocket.SendAsync(new ArraySegment<byte>(
        //         Encoding.UTF8.GetBytes("kernel-connected")),
        //         System.Net.WebSockets.WebSocketMessageType.Text,
        //         true,
        //         CancellationToken.None);

        //     var lastMessageReceived = DateTimeOffset.UtcNow;
        //     using var timer = new Timer(
        //         callback: _ =>
        //         {
        //             var period = DateTimeOffset.UtcNow - lastMessageReceived;
        //             if (period.TotalSeconds > 2)
        //             {
        //                 isConnected = false;
        //             }
        //         },
        //         state: null,
        //         dueTime: 0,
        //         period: 1000);

        //     while (!result.CloseStatus.HasValue && isConnected)
        //     {
        //         try
        //         {
        //             result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //             var command = Encoding.UTF8.GetString(buffer, 0, result.Count);
        //             if (command == "disconnect")
        //             {
        //                 isConnected = false;
        //             }

        //             await webSocket.SendAsync(new ArraySegment<byte>(
        //                 Encoding.UTF8.GetBytes("pong")),
        //                 System.Net.WebSockets.WebSocketMessageType.Text,
        //                 true,
        //                 CancellationToken.None);

        //             lastMessageReceived = DateTimeOffset.UtcNow;
        //         }
        //         catch
        //         {
        //             isConnected = false;
        //         }
        //     }

        //     await connectedClients.OnClientDisconnected(clientInformation.ConnectionId);
        //     _logger.ClientDisconnected(clientInformation.MicroserviceId, clientInformation.ConnectionId);

        //     if (result.CloseStatus is not null)
        //     {
        //         await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //     }
        // }
        // else
        // {
        //     HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        // }
    }
}
