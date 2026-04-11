// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectionLifecycle"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnectionLifecycle"/>.
/// </remarks>
/// <param name="logger">Logger for logging.</param>
public class ConnectionLifecycle(ILogger<ConnectionLifecycle> logger) : IConnectionLifecycle
{
    /// <summary>
    /// Adds or removes event handlers for when the connection is connected.
    /// </summary>
    public event Connected OnConnected = () => Task.CompletedTask;

    /// <summary>
    /// Adds or removes event handlers for when the connection is disconnected.
    /// </summary>
    public event Disconnected OnDisconnected = () => Task.CompletedTask;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; private set; } = ConnectionId.New();

    /// <inheritdoc/>
    public async Task Connected()
    {
        logger.Connected();

        var exceptions = new ConcurrentBag<Exception>();

        var tasks = OnConnected.GetInvocationList().Select(_ => Task.Run(async () =>
        {
            try
            {
                await ((Connected)_).Invoke();
            }
            catch (Exception ex)
            {
                logger.FailureDuringConnected(ex);
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        if (!exceptions.IsEmpty)
        {
            IsConnected = false;
            throw new AggregateException("One or more connection handlers failed", exceptions);
        }

        IsConnected = true;
    }

    /// <inheritdoc/>
    public async Task Disconnected()
    {
        IsConnected = false;
        logger.Disconnected();
        var tasks = OnDisconnected.GetInvocationList().Select(_ => Task.Run(async () =>
        {
            try
            {
                await ((Disconnected)_).Invoke();
            }
            catch (Exception ex)
            {
                logger.FailureDuringDisconnected(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        ConnectionId = ConnectionId.New();
    }
}
