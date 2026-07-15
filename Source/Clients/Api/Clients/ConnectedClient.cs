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
/// <param name="ProcessId">The identifier of the client process.</param>
/// <param name="ProcessPath">The full path of the client process executable.</param>
/// <param name="MachineName">The name of the machine the client process is running on.</param>
[ReadModel]
public record ConnectedClient(
    string SiloAddress,
    string ConnectionId,
    string Version,
    DateTimeOffset LastSeen,
    bool IsRunningWithDebugger,
    int ProcessId,
    string ProcessPath,
    string MachineName)
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

    /// <summary>
    /// Get the connected clients a specific observer's subscription delivers events to. Only
    /// client-owned observers have connected clients - for kernel-owned observers this is empty.
    /// </summary>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespace">The namespace within the event store the observer belongs to.</param>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="eventSequenceId">The event sequence the observer observes.</param>
    /// <param name="observers">The observers service to query for connected clients.</param>
    /// <returns>Collection of <see cref="ConnectedClient"/>.</returns>
    public static async Task<IEnumerable<ConnectedClient>> ConnectedClientsForObserver(
        string eventStore,
        string @namespace,
        string observerId,
        string eventSequenceId,
        Contracts.Observation.IObservers observers) =>
        (await observers.GetConnectedClientsForObserver(new()
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ObserverId = observerId,
            EventSequenceId = eventSequenceId
        })).ToApi();
}
