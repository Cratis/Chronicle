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
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
public sealed class DormantSiloDepartures(
    IDormantTargetSweep sweep,
    IClusterMembershipService clusterMembership,
    ILogger<DormantSiloDepartures> logger,
    TimeProvider? timeProvider = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    /// <summary>
    /// How often the membership is looked at.
    /// </summary>
    internal static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(10);

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
    /// <returns>The number of observers woken.</returns>
    internal async Task<int> CheckOnce()
    {
        var snapshot = clusterMembership.CurrentSnapshot;
        if (snapshot is null)
        {
            return 0;
        }

        var live = snapshot.Members.Values.Where(member => member.Status != SiloStatus.Dead).Select(member => member.SiloAddress).ToHashSet();
        var departed = _live.Where(silo => !live.Contains(silo)).ToArray();
        _live = live;
        if (departed.Length == 0)
        {
            return 0;
        }

        var woken = await sweep.SilosLeft([.. departed.Select(silo => silo.ToParsableString())]);
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

        await _cancellation.CancelAsync();
        try
        {
            await _loop;
        }
        catch (OperationCanceledException)
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
                    await CheckOnce();
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
