// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Clients;

/// <summary>
/// Represents a client connected to the Chronicle server.
/// </summary>
/// <param name="SiloAddress">The address of the server (silo) the client is connected to.</param>
/// <param name="ConnectionId">The unique connection identifier for the client.</param>
/// <param name="Version">The version of the client.</param>
/// <param name="LastSeen">The date and time the client was last seen.</param>
/// <param name="IsRunningWithDebugger">Whether or not the client is running with a debugger attached.</param>
[ReadModel]
public record ConnectedClient(
    string SiloAddress,
    string ConnectionId,
    string Version,
    DateTimeOffset LastSeen,
    bool IsRunningWithDebugger)
{
    /// <summary>
    /// Get all clients connected to the server, across all silos in the cluster.
    /// </summary>
    /// <param name="connectionService">The connection service to query for connected clients.</param>
    /// <returns>Collection of <see cref="ConnectedClient"/>.</returns>
    public static async Task<IEnumerable<ConnectedClient>> GetConnectedClients(IConnectionService connectionService) =>
        (await connectionService.GetConnectedClients()).ToApi();

    /// <summary>
    /// Get and observe all clients connected to the server, across all silos in the cluster.
    /// </summary>
    /// <param name="connectionService">The connection service to observe for changes in connected clients.</param>
    /// <returns>An observable of a collection of <see cref="ConnectedClient"/>.</returns>
    public static ISubject<IEnumerable<ConnectedClient>> AllConnectedClients(IConnectionService connectionService) =>
        connectionService.InvokeAndWrapWithTransformSubject(
            token => connectionService.ObserveConnectedClients(token),
            clients => clients.ToApi());
}
