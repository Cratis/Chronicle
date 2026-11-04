// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a system for the lifecycle of the client.
/// </summary>
public interface IConnectionLifecycle
{
    /// <summary>
    /// Adds or removes event handlers for when the connection is connected.
    /// </summary>
    event Connected OnConnected;

    /// <summary>
    /// Adds or removes event handlers for when the connection is disconnected.
    /// </summary>
    event Disconnected OnDisconnected;

    /// <summary>
    /// Gets whether, or not the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the current connection identifier.
    /// </summary>
    ConnectionId ConnectionId { get; }

    /// <summary>
    /// Called when the client gets connected to the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Connected();

    /// <summary>
    /// Completes when every disconnect in flight has finished running its handlers and the connection id has rotated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DisconnectCompleted() => Task.CompletedTask;

    /// <summary>
    /// Bounds every wait on the disconnect fence, so a disconnect handler that awaits a connect cannot deadlock it.
    /// </summary>
    /// <param name="timeout">How long a wait may last before it fails with <see cref="ConnectionTimedOut"/>.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> measuring the timeout.</param>
    /// <param name="cancellationToken">Connection lifetime token that abandons a wait.</param>
    void BoundDisconnectFence(TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
    }

    /// <summary>
    /// Called when the client is disconnected to the kernel.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Disconnected();
}
