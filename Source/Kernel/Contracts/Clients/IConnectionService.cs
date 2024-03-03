// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Aksio.Cratis.Kernel.Contracts.Clients;

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
    void ConnectionKeepAlive(ConnectionKeepAlive keepAlive);
}
