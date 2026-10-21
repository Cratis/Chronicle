// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a system for tracking the clients connected to a specific silo.
/// </summary>
/// <remarks>
/// The grain is keyed by the parsable string of the silo's <see cref="SiloAddress"/> and placed on
/// that silo, making the tracking local per silo. Use
/// <see cref="ConnectedClientsGrainFactoryExtensions.GetConnectedClients"/> to get the grain for a silo.
/// </remarks>
public interface IConnectedClients : IGrainWithStringKey
{
    /// <summary>
    /// Report that a client was connected.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="version">The version of the client.</param>
    /// <param name="isRunningWithDebugger">Whether or not the client is running with debugger.</param>
    /// <param name="processId">The identifier of the client process.</param>
    /// <param name="processPath">The full path of the client process executable.</param>
    /// <param name="machineName">The name of the machine the client process is running on.</param>
    /// <param name="clientType">The type of the client (for example, <c language="csharp">.NET</c>).</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientConnected(ConnectionId connectionId, string version, bool isRunningWithDebugger, int processId, string processPath, string machineName, string clientType);

    /// <summary>
    /// Put back a client the registry lost while its connection stayed open and the client kept responding.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="OnClientConnected"/> this is not a new connection: it fulfils no reservation, and a client the
    /// registry already knows is left exactly as it is.
    /// </remarks>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="version">The version of the client.</param>
    /// <param name="isRunningWithDebugger">Whether or not the client is running with debugger.</param>
    /// <param name="processId">The identifier of the client process.</param>
    /// <param name="processPath">The full path of the client process executable.</param>
    /// <param name="machineName">The name of the machine the client process is running on.</param>
    /// <param name="clientType">The type of the client (for example, <c language="csharp">.NET</c>).</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientRestored(ConnectionId connectionId, string version, bool isRunningWithDebugger, int processId, string processPath, string machineName, string clientType);

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
    /// Records projections declared by an existing connection. Unknown connections are ignored.
    /// </summary>
    /// <param name="connectionId">The declaring connection.</param>
    /// <param name="eventStore">The event store containing the projections.</param>
    /// <param name="projections">The declared projection identifiers.</param>
    /// <returns>Awaitable task.</returns>
    Task RecordDeclaredProjections(ConnectionId connectionId, EventStoreName eventStore, IEnumerable<ProjectionId> projections);

    /// <summary>
    /// Checks whether a currently connected client declares a projection.
    /// </summary>
    /// <param name="eventStore">The projection's event store.</param>
    /// <param name="projection">The projection identifier.</param>
    /// <returns>Whether a connected client declares the projection.</returns>
    Task<bool> DeclaresProjection(EventStoreName eventStore, ProjectionId projection);

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

    /// <summary>
    /// Gets the number of clients connected to this silo, including short-lived reservations from
    /// <see cref="ReserveConnection"/>.
    /// </summary>
    /// <returns>The number of connected clients.</returns>
    Task<int> GetConnectionCount();

    /// <summary>
    /// Reserve a connection slot ahead of a client actually connecting.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A least-connections client asks every candidate silo for its count, picks the lowest, and
    /// reserves a slot there before starting the real (comparatively slow) connect handshake - so a
    /// second client probing concurrently sees this pick reflected immediately instead of racing on
    /// a stale count and colliding on the same silo. <see cref="OnClientConnected"/> clears the
    /// oldest outstanding reservation when the real connection registers, so a successful connect
    /// does not double-count. If the connection attempt never completes (the client crashes, or
    /// picks a different silo after all), the reservation is never explicitly released - it simply
    /// expires on its own shortly after.
    /// </remarks>
    Task ReserveConnection();
}
