// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Grains.Clients;

/// <summary>
/// Defines a system for tracking connected observers.
/// </summary>
public interface IConnectedClients : IGrainWithIntegerKey
{
    /// <summary>
    /// Report that a client was connected.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="version">The version of the client.</param>
    /// <param name="isRunningWithDebugger">Whether or not the client is running with debugger.</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientConnected(ConnectionId connectionId, string version, bool isRunningWithDebugger);

    /// <summary>
    /// Report that a client was disconnected.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="reason">Reason for disconnected.</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientDisconnected(ConnectionId connectionId, string reason);

    /// <summary>
    /// Register that the client was seen.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <returns>True if connection is considered connected, false if not.</returns>
    Task<bool> OnClientPing(ConnectionId connectionId);

    /// <summary>
    /// Check whether or not a client by its <see cref="ConnectionId"/> is connected.
    /// </summary>
    /// <param name="connectionId">The <see cref="ConnectionId"/> to check.</param>
    /// <returns>True if connected, false if not.</returns>
    Task<bool> IsConnected(ConnectionId connectionId);

    /// <summary>
    /// Gets the <see cref="ConnectedClient"/> from the <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="connectionId"><see cref="ConnectionId"/> to get for.</param>
    /// <returns>The <see cref="ConnectedClient"/> associated with the <see cref="ConnectionId"/>.</returns>
    Task<ConnectedClient> GetConnectedClient(ConnectionId connectionId);

    /// <summary>
    /// Gets all the connected clients.
    /// </summary>
    /// <returns>A collection of <see cref="ConnectedClient"/>.</returns>
    Task<IEnumerable<ConnectedClient>> GetAllConnectedClients();
}
