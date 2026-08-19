// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Reactive;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents the read model for a client connected to the cluster.
/// </summary>
/// <param name="Id">The unique identifier of the connection.</param>
/// <param name="SiloAddress">The address of the silo terminating the connection.</param>
/// <param name="ConnectionId">The identifier of the connection.</param>
/// <param name="Version">The version of the client.</param>
/// <param name="LastSeen">When the client was last heard from.</param>
/// <param name="IsRunningWithDebugger">Whether the client runs with a debugger attached.</param>
/// <param name="ProcessId">The process identifier of the client.</param>
/// <param name="ProcessPath">The path of the process running the client.</param>
/// <param name="MachineName">The name of the machine running the client.</param>
/// <param name="ClientType">The type of client.</param>
[ReadModel]
public record ConnectedClientDetails(
    string Id,
    string SiloAddress,
    string ConnectionId,
    string Version,
    DateTimeOffset LastSeen,
    bool IsRunningWithDebugger,
    int ProcessId,
    string ProcessPath,
    string MachineName,
    string ClientType)
{
    /// <summary>
    /// Gets every client connected to the server, across all silos in the cluster.
    /// </summary>
    /// <param name="clients">The <see cref="ConnectedClientsQuery"/> holding the cluster view.</param>
    /// <returns>A collection of <see cref="ConnectedClientDetails"/>.</returns>
    internal static async Task<IEnumerable<ConnectedClientDetails>> GetConnectedClients(ConnectedClientsQuery clients) =>
        (await clients.GetAll()).ToReadModel();

    /// <summary>
    /// Observes every client connected to the server, across all silos in the cluster.
    /// </summary>
    /// <param name="clients">The <see cref="ConnectedClientsQuery"/> holding the cluster view.</param>
    /// <returns>An observable subject emitting collections of <see cref="ConnectedClientDetails"/>.</returns>
    internal static ISubject<IEnumerable<ConnectedClientDetails>> AllConnectedClients(ConnectedClientsQuery clients) =>
        clients.InvokeAndWrapWithTransformSubject(
            clients.ObserveAll,
            connected => connected.ToReadModel());

    /// <summary>
    /// Gets the clients a specific observer's subscription delivers events to.
    /// </summary>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="namespace">The namespace within the event store the observer belongs to.</param>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="eventSequenceId">The event sequence the observer observes.</param>
    /// <param name="clients">The <see cref="ConnectedClientsQuery"/> holding the cluster view.</param>
    /// <returns>A collection of <see cref="ConnectedClientDetails"/>.</returns>
    /// <remarks>
    /// Only client-owned observers have connected clients - for kernel-owned observers this is empty.
    /// </remarks>
    internal static async Task<IEnumerable<ConnectedClientDetails>> ConnectedClientsForObserver(
        string eventStore,
        string @namespace,
        string observerId,
        string eventSequenceId,
        ConnectedClientsQuery clients) =>
        (await clients.GetAllForObserver(eventStore, @namespace, observerId, eventSequenceId)).ToReadModel();
}
