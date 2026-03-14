// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a manager for active client connections that supports coordinated
/// disconnect-and-block during Chronicle reset operations.
/// </summary>
public interface IClientConnectionManager
{
    /// <summary>
    /// Register a client connection so it can be forcibly disconnected later.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> that controls the connection's keep-alive loop.</param>
    void Register(ConnectionId connectionId, CancellationTokenSource cancellationTokenSource);

    /// <summary>
    /// Unregister a client connection after it has disconnected.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    void Unregister(ConnectionId connectionId);

    /// <summary>
    /// Disconnect all currently connected clients by cancelling their keep-alive loops.
    /// </summary>
    /// <param name="reason">A human-readable reason for the disconnection.</param>
    void DisconnectAll(string reason);

    /// <summary>
    /// Block new connections from proceeding until <see cref="AllowConnections"/> is called.
    /// </summary>
    void BlockConnections();

    /// <summary>
    /// Allow new connections to proceed, releasing any callers waiting in <see cref="WaitUntilAcceptingConnections"/>.
    /// </summary>
    void AllowConnections();

    /// <summary>
    /// Wait asynchronously until the manager is accepting new connections.
    /// Returns immediately when connections are not blocked.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task WaitUntilAcceptingConnections();
}
