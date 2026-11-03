// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Runs recoverable startup work in the background, retrying each item until it succeeds or the host stops.
/// </summary>
/// <remarks>
/// <para>
/// Backlog-proportional work, such as activating every persisted observer, must not gate readiness and must not
/// crash the host when it is slow or failing: a restart meets the same backlog. Each item is therefore retried with
/// capped exponential backoff and jitter. Once an item has failed <see cref="EscalateAfterFailures"/> times in a
/// row it is escalated through the <see cref="IStartupWorkEscalation"/> seam, then again at most once per
/// <see cref="MaxDelay"/> while it keeps failing.
/// </para>
/// <para>
/// Only one run of a given <see cref="StartupWorkItem"/> is in flight at a time, so the same observer is never
/// activated by two overlapping attempts from the startup pass. The work itself must be idempotent.
/// </para>
/// <para>
/// At most <see cref="DefaultMaxConcurrentAttempts"/> attempts run at the same time across all items, so a store with
/// many namespaces and observers does not flood the cluster and storage while live traffic is already arriving. The
/// slot is held only for the attempt itself, never during the backoff, so a persistently failing item cannot starve the
/// others.
/// </para>
/// </remarks>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the backoff waits on.</param>
/// <param name="escalation">The <see cref="IStartupWorkEscalation"/> told about persistent failure and recovery.</param>
/// <param name="logger">The logger.</param>
/// <param name="jitter">Optional source of values in [0, 1) for the backoff jitter; defaults to <see cref="Random.Shared"/>.</param>
/// <param name="maxConcurrentAttempts">Optional limit on attempts running at the same time; defaults to <see cref="DefaultMaxConcurrentAttempts"/>.</param>
#pragma warning disable CA1001 // The semaphore never exposes its wait handle, holds no unmanaged resources and lives as long as the host.
internal sealed class RecoverableStartupWork(
    TimeProvider timeProvider,
    IStartupWorkEscalation escalation,
    ILogger<ChronicleServerStartupTask> logger,
    Func<double>? jitter = null,
    int maxConcurrentAttempts = RecoverableStartupWork.DefaultMaxConcurrentAttempts)
{
    /// <summary>
    /// The default limit on attempts running at the same time across all items.
    /// </summary>
    internal const int DefaultMaxConcurrentAttempts = 64;

    /// <summary>
    /// The number of consecutive failures after which an item is escalated.
    /// </summary>
    internal const int EscalateAfterFailures = 5;

    /// <summary>
    /// The backoff before the second attempt.
    /// </summary>
    internal static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The longest backoff, and the minimum interval between repeated escalations of the same item.
    /// </summary>
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);

    readonly ConcurrentDictionary<StartupWorkItem, byte> _inFlight = new();
    readonly Func<double> _jitter = jitter ?? Random.Shared.NextDouble;
    readonly SemaphoreSlim _attempts = new(maxConcurrentAttempts, maxConcurrentAttempts);

    /// <summary>
    /// Runs an item until it succeeds or <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="item">The <see cref="StartupWorkItem"/> identifying the work.</param>
    /// <param name="work">The idempotent work to perform.</param>
    /// <param name="cancellationToken">Token that cancels the work and the backoff when the host stops.</param>
    /// <returns>Awaitable task that completes on success or cancellation and never throws for a failing item.</returns>
    public async Task Run(StartupWorkItem item, Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        if (!_inFlight.TryAdd(item, 0))
        {
            return;
        }

        try
        {
            var failures = 0;
            long? lastEscalation = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _attempts.WaitAsync(cancellationToken);
                    try
                    {
                        await work(cancellationToken).WaitAsync(cancellationToken);
                    }
                    finally
                    {
                        _attempts.Release();
                    }

                    if (lastEscalation is not null)
                    {
                        escalation.Recovered(item, failures + 1);
                    }

                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    failures++;
                    var delay = NextDelay(failures);
                    logger.RecoverableStartupWorkFailed(exception, item.Kind, item.Key, failures, delay);

                    if (failures >= EscalateAfterFailures &&
                        (lastEscalation is null || timeProvider.GetElapsedTime(lastEscalation.Value) >= MaxDelay))
                    {
                        lastEscalation = timeProvider.GetTimestamp();
                        escalation.Escalate(item, failures, delay, exception);
                    }

                    try
                    {
                        await Task.Delay(delay, timeProvider, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
        finally
        {
            _inFlight.TryRemove(item, out _);
        }
    }

    /// <summary>
    /// Computes the backoff after a number of consecutive failures: exponential up to <see cref="MaxDelay"/>, then jittered between half and all of that ceiling.
    /// </summary>
    /// <param name="failures">The number of consecutive failures so far, at least one.</param>
    /// <returns>The delay before the next attempt, never above <see cref="MaxDelay"/>.</returns>
    internal TimeSpan NextDelay(int failures)
    {
        var ceiling = TimeSpan.FromTicks((long)Math.Min(MaxDelay.Ticks, BaseDelay.Ticks * Math.Pow(2, Math.Min(failures - 1, 10))));

        // Never less than half the ceiling, so an unlucky draw cannot turn the retry into a tight spin.
        var half = ceiling / 2;
        return half + (half * Math.Clamp(_jitter(), 0d, 1d));
    }
}
#pragma warning restore CA1001
