// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Api;

/// <summary>
/// Represents the endpoint for clients to connect to.
/// </summary>
[Route("/api/clients")]
public class ConnectedClients : Controller
{
    readonly ILogger<ConnectedClients> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(ILogger<ConnectedClients> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Accepts client connections over Web Sockets.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [HttpGet]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            _logger.ClientConnected();

            var isConnected = true;
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

            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue && isConnected)
            {
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch
                {
                    isConnected = false;
                }
            }

            _logger.ClientDisconnected();

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
