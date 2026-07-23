// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Tasks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Watches the keep-alive heartbeat of a Chronicle connection and drives reconnection when the
/// session drops, retrying with capped exponential backoff when a reconnect attempt itself fails.
/// </summary>
/// <remarks>
/// A reconnect attempt can fail before a fresh channel is even dialed - address resolution,
/// load-balancer selection, or channel creation can throw, typically under the very network
/// conditions that dropped the session in the first place. Such a failure must never end
/// reconnection permanently; the watchdog keeps retrying until an attempt completes or the
/// client shuts down.
/// </remarks>
/// <param name="tasks"><see cref="ITaskFactory"/> to run the monitor and delays with.</param>
/// <param name="sessionDropped">Callback invoked when the keep-alive goes stale, before reconnecting.</param>
/// <param name="reconnect">Callback performing a full reconnect - resolving addresses, dialing a fresh channel and re-establishing the session.</param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/> for diagnostics.</param>
/// <param name="cancellationToken">The client's <see cref="CancellationToken"/> - cancelling it stops the watchdog.</param>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/> for staleness decisions. Defaults to the system clock.</param>
public class ConnectionWatchdog(
    ITaskFactory tasks,
    Func<Task> sessionDropped,
    Func<Task> reconnect,
    ILogger<ConnectionWatchdog> logger,
    CancellationToken cancellationToken,
    TimeProvider? timeProvider = null)
{
    const int MonitorIntervalMilliseconds = 1000;

    static readonly TimeSpan _keepAliveStaleAfter = TimeSpan.FromSeconds(5);
    static readonly TimeSpan _maxReconnectBackoff = TimeSpan.FromSeconds(30);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    DateTimeOffset _lastKeepAlive = (timeProvider ?? TimeProvider.System).GetUtcNow();
    int _running;

    /// <summary>
    /// Records that a keep-alive was observed, marking the session as healthy.
    /// </summary>
    public void NotifyKeepAlive() => _lastKeepAlive = _timeProvider.GetUtcNow();

    /// <summary>
    /// Starts monitoring the keep-alive heartbeat. Idempotent - a monitor that is already
    /// running is left alone.
    /// </summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }

        _ = tasks.Run(RunAsync, cancellationToken);
    }

    static TimeSpan BackoffFor(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(_maxReconnectBackoff.TotalSeconds, Math.Pow(2, attempt - 1)));

    async Task RunAsync()
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await tasks.Delay(MonitorIntervalMilliseconds, cancellationToken);
                if (_timeProvider.GetUtcNow() - _lastKeepAlive <= _keepAliveStaleAfter)
                {
                    continue;
                }

                await NotifySessionDropped();
                await ReconnectWithRetry();
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // Expected on shutdown or disposal - let the monitor exit.
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    async Task NotifySessionDropped()
    {
        try
        {
            await sessionDropped();
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
        {
            // A failing drop notification must not prevent the reconnect that follows.
            logger.SessionDroppedNotificationFailed(ex);
        }
    }

    async Task ReconnectWithRetry()
    {
        for (var attempt = 1; !cancellationToken.IsCancellationRequested; attempt++)
        {
            logger.Reconnecting(attempt);
            try
            {
                await reconnect();
                return;
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var backoff = BackoffFor(attempt);
                logger.ReconnectAttemptFailed(attempt, backoff.TotalSeconds, ex);
                await tasks.Delay((int)backoff.TotalMilliseconds, cancellationToken);
            }
        }
    }
}
