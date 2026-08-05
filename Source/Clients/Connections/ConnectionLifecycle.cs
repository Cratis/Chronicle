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
    readonly Lock _handlers = new();
    Connected _onConnected = Nothing;
    Disconnected _onDisconnected = Nothing;

    /// <summary>
    /// Adds or removes event handlers for when the connection is connected.
    /// </summary>
    /// <remarks>
    /// A handler already subscribed is not subscribed again. These handlers are whole-artifact registrations - the
    /// event store's <c>RegisterAll</c> is one of them - so running one twice re-registers every event type,
    /// constraint and seeding on every reconnect. Nothing about a second subscription expresses an intent to do
    /// that, and a caller who cannot be sure whether it already subscribed cannot express the intent not to, so
    /// subscribing is idempotent. Removing still removes.
    /// </remarks>
    public event Connected OnConnected
    {
        add
        {
            lock (_handlers)
            {
                if (value is null || Array.IndexOf(_onConnected.GetInvocationList(), value) >= 0)
                {
                    return;
                }

                _onConnected += value;
            }
        }

        remove
        {
            lock (_handlers)
            {
                // Removing the last handler leaves nothing to invoke, so fall back to the no-op the field started
                // as rather than to null - Connected() invokes this without checking.
                _onConnected = (Connected?)Delegate.Remove(_onConnected, value) ?? Nothing;
            }
        }
    }

    /// <summary>
    /// Adds or removes event handlers for when the connection is disconnected.
    /// </summary>
    /// <remarks>
    /// A handler already subscribed is not subscribed again, for the same reason as <see cref="OnConnected"/>.
    /// </remarks>
    public event Disconnected OnDisconnected
    {
        add
        {
            lock (_handlers)
            {
                if (value is null || Array.IndexOf(_onDisconnected.GetInvocationList(), value) >= 0)
                {
                    return;
                }

                _onDisconnected += value;
            }
        }

        remove
        {
            lock (_handlers)
            {
                _onDisconnected = (Disconnected?)Delegate.Remove(_onDisconnected, value) ?? Nothing;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public ConnectionId ConnectionId { get; private set; } = ConnectionId.New();

    /// <inheritdoc/>
    public async Task Connected()
    {
        // Set IsConnected = true before invoking handlers so that any handler that accesses
        // services via IChronicleServicesAccessor does not deadlock on the connection lock.
        IsConnected = true;

        logger.Connected();

        var exceptions = new ConcurrentBag<Exception>();

        var tasks = _onConnected.GetInvocationList().Select(_ => Task.Run(async () =>
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
            // One or more handlers failed. Roll back IsConnected so callers cannot assume
            // the client is fully operational, and surface all failures to the caller.
            IsConnected = false;
            throw new AggregateException(exceptions);
        }
    }

    /// <inheritdoc/>
    public async Task Disconnected()
    {
        IsConnected = false;
        logger.Disconnected();
        var tasks = _onDisconnected.GetInvocationList().Select(_ => Task.Run(async () =>
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

    static Task Nothing() => Task.CompletedTask;
}
