// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Defines a grain representing one client connection, keyed by its <see cref="ConnectionId"/>.
/// </summary>
/// <remarks>
/// The <see cref="IConnectedClients"/> registry stays the authority and drives <see cref="Connect"/> and <see cref="Disconnect"/>.
/// An epoch identifies one connected period of the same <see cref="ConnectionId"/>, since a restored client reuses its identifier.
/// </remarks>
public interface IClientConnection : IGrainWithStringKey
{
    /// <summary>
    /// Mark the connection as connected. Idempotent. Reconnecting after a disconnect starts a new epoch.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Connect();

    /// <summary>
    /// Mark the connection as disconnected and notify all subscribers once. Idempotent.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// One-way, so the registry's turn never waits on the fan-out to subscribers.
    /// </remarks>
    [OneWay]
    Task Disconnect();

    /// <summary>
    /// Subscribe to be told when the connection disconnects. Idempotent.
    /// </summary>
    /// <param name="subscriber">The <see cref="GrainId"/> of the <see cref="IClientDisconnectionSubscriber"/>.</param>
    /// <returns>The current <see cref="ClientConnectionEpoch"/>, or <see cref="ClientConnectionEpoch.NotSet"/> if the connection is already disconnected.</returns>
    Task<ClientConnectionEpoch> Subscribe(GrainId subscriber);

    /// <summary>
    /// Stop being told when the connection disconnects.
    /// </summary>
    /// <param name="subscriber">The <see cref="GrainId"/> of the subscriber.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(GrainId subscriber);
}
