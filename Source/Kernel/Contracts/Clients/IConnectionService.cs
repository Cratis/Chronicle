// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Clients;

/// <summary>
/// Defines a system that manages client connections to Cratis.
/// </summary>
[Service]
public interface IConnectionService
{
    /// <summary>
    /// Connect to the server.
    /// </summary>
    /// <param name="request"><see cref="ConnectRequest"/> to use when connecting.</param>
    /// <param name="context">The <see cref="CallContext"/> for the call.</param>
    /// <returns>Observable that contains a stream of pings.</returns>
    /// <remarks>
    /// The server will push pings to the client.
    /// </remarks>
    [Operation]
    IObservable<ConnectionKeepAlive> Connect(ConnectRequest request, CallContext context = default);

    /// <summary>
    /// Notify server the client is still alive.
    /// </summary>
    /// <param name="keepAlive"><see cref="ConnectionKeepAlive"/> with information about the client.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Operation]
    Task ConnectionKeepAlive(ConnectionKeepAlive keepAlive);

    /// <summary>
    /// Get the FileDescriptorSet for all services exposed by the server.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="DescriptorSetResponse"/>.</returns>
    [Operation]
    Task<DescriptorSetResponse> GetDescriptorSet();

    /// <summary>
    /// Check whether the server still serves the contracts a client expects.
    /// </summary>
    /// <param name="request"><see cref="CompatibilityRequest"/> carrying the client's descriptor set and versions.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="CompatibilityResponse"/>.</returns>
    /// <remarks>
    /// The check lives here rather than in each client because the clients are written in four languages and the
    /// comparison should not be four implementations that can disagree. A client sends what it expects; the server
    /// answers with what it serves and, when those differ, with what differs.
    /// </remarks>
    [Operation]
    Task<CompatibilityResponse> CheckCompatibility(CompatibilityRequest request);

    /// <summary>
    /// Get all clients connected to the server, across all silos in the cluster.
    /// </summary>
    /// <param name="context">The <see cref="CallContext"/> for the call.</param>
    /// <returns>A collection of <see cref="ConnectedClient"/>.</returns>
    [Operation]
    Task<IEnumerable<ConnectedClient>> GetConnectedClients(CallContext context = default);

    /// <summary>
    /// Observe all clients connected to the server, across all silos in the cluster.
    /// </summary>
    /// <param name="context">The <see cref="CallContext"/> for the call.</param>
    /// <returns>An observable of a collection of <see cref="ConnectedClient"/>.</returns>
    [Operation]
    IObservable<IEnumerable<ConnectedClient>> ObserveConnectedClients(CallContext context = default);
}
