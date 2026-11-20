// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Sweeps the durable subscriptions of dormant observers when a silo leaves the cluster.
/// </summary>
/// <remarks>
/// <para>
/// A client connected to a silo that dies is gone with it, and an observer released while dormant has no activation to
/// notice. Every silo watches the membership and, when one is declared dead, reads the durable subscriptions for the observers
/// that recorded a client served from it and wakes each, so it validates what the record names and drops what is gone. The
/// sweep is idempotent and every silo runs it, so no coordination is needed; the safety net of the queues finds whatever a sweep
/// misses within <see cref="Configuration.Observers.DormantSafetyNet"/> (decision 0034).
/// </para>
/// </remarks>
/// <param name="sweep">The <see cref="IDormantTargetSweep"/> that wakes the observers.</param>
/// <param name="clusterMembership">The <see cref="IClusterMembershipService"/> that tells when a silo leaves.</param>
/// <param name="localSilo">The <see cref="ILocalSiloDetails"/> of this silo; a silo that sees itself leave is shutting down and sweeps nothing.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
public sealed class DormantSiloDepartures(
    IDormantTargetSweep sweep,
    IClusterMembershipService clusterMembership,
    ILocalSiloDetails localSilo,
    ILogger<DormantSiloDepartures> logger,
    TimeProvider? timeProvider = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    /// <summary>
    /// How often the membership is looked at.
    /// </summary>
    internal static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long a stop waits for a sweep that is cancelled to return.
    /// </summary>
    internal static readonly TimeSpan StopWait = TimeSpan.FromSeconds(1);

    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    HashSet<SiloAddress> _live = [];
    CancellationTokenSource? _cancellation;
    Task _loop = Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => _cancellation?.Dispose();

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(DormantSiloDepartures), ServiceLifecycleStage.Active, Start, Stop);

    /// <summary>
    /// Looks at the membership once and sweeps for the silos that left since the last look.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that stops the sweep when the silo is stopping.</param>
    /// <returns>The number of observers woken.</returns>
    internal async Task<int> CheckOnce(CancellationToken cancellationToken = default)
    {
        var snapshot = clusterMembership.CurrentSnapshot;
        if (snapshot is null)
        {
            return 0;
        }

        var live = snapshot.Members.Values.Where(member => member.Status != SiloStatus.Dead).Select(member => member.SiloAddress).ToHashSet();
        var departed = _live.Where(silo => !live.Contains(silo)).ToArray();
        _live = live;

        // A silo that finds itself among the departed is stopping, and what it would wake is going down with it.
        if (departed.Length == 0 || departed.Contains(localSilo.SiloAddress) || cancellationToken.IsCancellationRequested)
        {
            return 0;
        }

        var woken = await sweep.SilosLeft([.. departed.Select(silo => silo.ToParsableString())], cancellationToken);
        logger.DormantObserversSweptForDepartedSilos(string.Join(", ", departed.Select(silo => silo.ToString())), woken);
        return woken;
    }

    Task Start(CancellationToken cancellationToken)
    {
        _live = clusterMembership.CurrentSnapshot?.Members.Values.Where(member => member.Status != SiloStatus.Dead).Select(member => member.SiloAddress).ToHashSet() ?? [];
        _cancellation = new();
        _loop = DetachedExecution.Run(() => Loop(_cancellation.Token));
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        if (_cancellation is null)
        {
            return;
        }

        // A sweep that is still calling observers must not hold the silo's shutdown up: it is cancelled and given a moment, and
        // the next silo to look finds what it missed.
        await _cancellation.CancelAsync();
        try
        {
            await _loop.WaitAsync(StopWait, _time, cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            // Stopping.
        }
    }

    async Task Loop(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(CheckEvery, _time);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await CheckOnce(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.FailedToCheckForDepartedSilos(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }
}
