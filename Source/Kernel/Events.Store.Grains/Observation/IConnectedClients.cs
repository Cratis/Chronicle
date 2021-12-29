// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Microsoft.AspNetCore.Connections;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Delegate for when a client is disconnected.
    /// </summary>
    /// <param name="context">The client <see cref="ConnectionContext"/>.</param>
    public delegate Task ClientDisconnected(ConnectionContext context);

    /// <summary>
    /// Defines a system for tracking connected observers.
    /// </summary>
    public interface IConnectedClients
    {
        /// <summary>
        /// Gets the number of connected clients.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets whether or not there are any connected clients.
        /// </summary>
        bool AnyConnectedClients { get; }

        /// <summary>
        /// The event that is triggered when a client is disconnected.
        /// </summary>
        event ClientDisconnected ClientDisconnected;

        /// <summary>
        /// Report that a client was connected.
        /// </summary>
        /// <param name="context">The context of the client.</param>
        /// <returns>Awaitable task.</returns>
        Task OnClientConnected(ConnectionContext context);

        /// <summary>
        /// Report that a client was disconnected.
        /// </summary>
        /// <param name="context">The context of the client.</param>
        /// <returns>Awaitable task.</returns>
        Task OnClientDisconnected(ConnectionContext context);
    }
}
