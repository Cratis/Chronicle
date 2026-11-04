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
    /// Guards the two handler lists.
    /// </summary>
    /// <remarks>
    /// Deliberately an <see cref="object"/> rather than a <c language="csharp">System.Threading.Lock</c>: this assembly is packed for
    /// net8.0 as well, and that type is net9.0 and later. The net10.0 build the gate runs would not have noticed.
    /// </remarks>
    readonly object _handlers = new();
    readonly object _generationGate = new();
    Task _disconnecting = Task.CompletedTask;
    Connected _onConnected = Nothing;
    Disconnected _onDisconnected = Nothing;

    /// <summary>
    /// Adds or removes event handlers for when the connection is connected.
    /// </summary>
    /// <remarks>
    /// A handler already subscribed is not subscribed again. These handlers are whole-artifact registrations - the
    /// event store's <c language="csharp">RegisterAll</c> is one of them - so running one twice re-registers every event type,
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
    /// <remarks>
    /// Generations are ordered: the <c language="csharp">Disconnected</c> callbacks of one generation complete and the
    /// <see cref="ConnectionId"/> rotates before the next <c language="csharp">Connected</c> sets <see cref="IsConnected"/> and runs its
    /// handlers. A disconnect that starts after <c language="csharp">Connected</c> has set <see cref="IsConnected"/> overlaps that
    /// generation's own handlers, which is accepted; the generation after it is fenced behind that disconnect.
    /// </remarks>
    public async Task Connected()
    {
        await WaitForPreviousDisconnect();

        logger.Connected();

        var exceptions = new ConcurrentBag<Exception>();

        var tasks = _onConnected.GetInvocationList().Select(_ => ExecutionContextDetached.Start(() => Task.Run(async () =>
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
        }))).ToArray();

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
    public Task Disconnected()
    {
        lock (_generationGate)
        {
            IsConnected = false;

            // Handlers are dispatched through Task.Run, so none of them runs inline under the gate.
            var run = RunDisconnected();
            _disconnecting = _disconnecting.IsCompleted ? run : Task.WhenAll(_disconnecting, run);
            return run;
        }
    }

    static Task Nothing() => Task.CompletedTask;

    async Task WaitForPreviousDisconnect()
    {
        // IsConnected is set before the handlers run so that a handler accessing services through
        // IChronicleServicesAccessor does not deadlock on the connection lock.
        while (true)
        {
            Task fence;
            lock (_generationGate)
            {
                if (_disconnecting.IsCompleted)
                {
                    IsConnected = true;
                    return;
                }

                fence = _disconnecting;
            }

            logger.WaitingForDisconnectToComplete();
            await fence;
        }
    }

    async Task RunDisconnected()
    {
        logger.Disconnected();
        var tasks = _onDisconnected.GetInvocationList().Select(_ => ExecutionContextDetached.Start(() => Task.Run(async () =>
        {
            try
            {
                await ((Disconnected)_).Invoke();
            }
            catch (Exception ex)
            {
                logger.FailureDuringDisconnected(ex);
            }
        }))).ToArray();

        await Task.WhenAll(tasks);

        ConnectionId = ConnectionId.New();
    }
}
