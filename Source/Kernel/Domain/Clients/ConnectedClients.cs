// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Kernel.Grains.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Domain.Clients;

/// <summary>
/// Represents the endpoint for clients to connect to.
/// </summary>
[Route("/api/clients")]
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
    /// <returns>Awaitable task.</returns>
    [HttpGet("ping")]
    public Task Ping() => Task.CompletedTask;

    /// <summary>
    /// Accepts client connections over Web Sockets.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [HttpGet]
    public async Task Connect()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            var isConnected = true;
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var clientInformation = JsonSerializer.Deserialize<ClientInformation>(json, _jsonSerializerOptions)!;
            _logger.ClientConnected(clientInformation.ClientVersion, clientInformation.MicroserviceId, clientInformation.ConnectionId);

            var connectedClients = _grainFactory.GetGrain<IConnectedClients>(clientInformation.MicroserviceId);
            await connectedClients.OnClientConnected(
                clientInformation.ConnectionId,
                new Uri(clientInformation.AdvertisedUri),
                clientInformation.ClientVersion);

            await webSocket.SendAsync(new ArraySegment<byte>(
                Encoding.UTF8.GetBytes("kernel-connected")),
                System.Net.WebSockets.WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            var lastMessageReceived = DateTimeOffset.UtcNow;
            using var timer = new Timer(
                callback: _ =>
                {
                    var period = DateTimeOffset.UtcNow - lastMessageReceived;
                    if (period.TotalSeconds > 2)
                    {
                        isConnected = false;
                    }
                },
                state: null,
                dueTime: 0,
                period: 1000);

            while (!result.CloseStatus.HasValue && isConnected)
            {
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var command = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (command == "disconnect")
                    {
                        isConnected = false;
                    }

                    await webSocket.SendAsync(new ArraySegment<byte>(
                        Encoding.UTF8.GetBytes("pong")),
                        System.Net.WebSockets.WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                    lastMessageReceived = DateTimeOffset.UtcNow;
                }
                catch
                {
                    isConnected = false;
                }
            }

            await connectedClients.OnClientDisconnected(clientInformation.ConnectionId);
            _logger.ClientDisconnected(clientInformation.MicroserviceId, clientInformation.ConnectionId);

            if (result.CloseStatus is not null)
            {
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
