// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.InProcess;

/// <summary>
/// Represents an implementation of <see cref="IConnectionLifecycle"/> for in-process scenarios.
/// This implementation does not trigger OnConnected/OnDisconnected events since there is no actual connection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InProcessConnectionLifecycle"/>.
/// </remarks>
/// <param name="logger">Logger for logging.</param>
public class InProcessConnectionLifecycle(ILogger<InProcessConnectionLifecycle> logger) : IConnectionLifecycle
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
    public bool IsConnected { get; private set; } = true; // In-process is always "connected"

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; private set; } = ConnectionId.New();

    /// <inheritdoc/>
    public Task Connected()
    {
        IsConnected = true;
        logger.LogDebug("In-process connection marked as connected, but no OnConnected events will be triggered");
        // Intentionally NOT triggering OnConnected events to avoid double registration
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Disconnected()
    {
        IsConnected = false;
        logger.LogDebug("In-process connection marked as disconnected, but no OnDisconnected events will be triggered");
        // Intentionally NOT triggering OnDisconnected events since in-process doesn't have real disconnections
        ConnectionId = ConnectionId.New();
        return Task.CompletedTask;
    }
}