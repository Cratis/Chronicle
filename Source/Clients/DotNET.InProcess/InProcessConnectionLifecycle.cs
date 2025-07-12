// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.InProcess;

/// <summary>
/// Represents an implementation of <see cref="IConnectionLifecycle"/> for in-process scenarios.
/// This implementation does not trigger OnConnected/OnDisconnected events since there is no actual connection.
/// </summary>
public class InProcessConnectionLifecycle : IConnectionLifecycle
{
    /// <summary>
    /// Adds or removes event handlers for when the connection is connected.
    /// In-process clients don't have real connections, so this event is never triggered.
    /// </summary>
    public event Connected OnConnected = () => Task.CompletedTask;

    /// <summary>
    /// Adds or removes event handlers for when the connection is disconnected.
    /// In-process clients don't have real connections, so this event is never triggered.
    /// </summary>
    public event Disconnected OnDisconnected = () => Task.CompletedTask;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; private set; } = ConnectionId.New();

    /// <inheritdoc/>
    public Task Connected()
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Disconnected()
    {
        IsConnected = false;
        ConnectionId = ConnectionId.New();
        return Task.CompletedTask;
    }
}
