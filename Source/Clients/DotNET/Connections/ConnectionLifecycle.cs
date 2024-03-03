// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectionLifecycle"/>.
/// </summary>
public class ConnectionLifecycle : IConnectionLifecycle
{
    readonly ILogger<ConnectionLifecycle> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionLifecycle"/>.
    /// </summary>
    /// <param name="logger">Logger for logging.</param>
    public ConnectionLifecycle(ILogger<ConnectionLifecycle> logger)
    {
        _logger = logger;
        ConnectionId = ConnectionId.New();
    }

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
    public ConnectionId ConnectionId { get; private set; }

    /// <inheritdoc/>
    public async Task Connected()
    {
        IsConnected = true;

        _logger.Connected();

        var tasks = OnConnected.GetInvocationList().Select(_ => Task.Run(async () =>
        {
            try
            {
                await ((Connected)_).Invoke();
            }
            catch (Exception ex)
            {
                _logger.FailureDuringConnected(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task Disconnected()
    {
        IsConnected = false;

        _logger.Disconnected();

        var tasks = OnDisconnected.GetInvocationList().Select(_ => Task.Run(async () =>
        {
            try
            {
                await ((Disconnected)_).Invoke();
            }
            catch (Exception ex)
            {
                _logger.FailureDuringDisconnected(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        ConnectionId = ConnectionId.New();
    }
}
