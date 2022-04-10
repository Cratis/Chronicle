// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Defines a system for tracking connected observers.
/// </summary>
public interface IConnectedClients : IGrainWithGuidKey
{
    /// <summary>
    /// Report that a client was connected.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientConnected(string connectionId);

    /// <summary>
    /// Report that a client was disconnected.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <returns>Awaitable task.</returns>
    Task OnClientDisconnected(string connectionId);

    /// <summary>
    /// Subscribe to disconnected connection event.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="observer">Observer to call.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeOnDisconnected(string connectionId, IConnectedClientObserver observer);

    /// <summary>
    /// Unsubscribe to disconnected connection event.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="observer">Observer to call.</param>
    /// <returns>Awaitable task.</returns>
    Task UnsubscribeOnDisconnected(string connectionId, IConnectedClientObserver observer);
}
