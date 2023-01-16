// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Clients;

/// <summary>
/// Defines a connection using Web Sockets.
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    /// <summary>
    /// Gets the unique <see cref="ConnectionId"/> for the client.
    /// </summary>
    ConnectionId ConnectionId { get; }

    /// <summary>
    /// Gets whether or not the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the kernel web socket API.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Connect();
}
