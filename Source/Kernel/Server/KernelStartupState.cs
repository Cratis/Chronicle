// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Tracks readiness after every StartedAsync, including the silo's Active lifecycle stage, has completed.
/// </summary>
/// <param name="lifetime">The host application lifetime.</param>
/// <param name="options">The Chronicle configuration.</param>
/// <param name="leases">The single-writer lease, when this kernel uses one.</param>
/// <param name="logger">The logger.</param>
internal sealed class KernelStartupState(
    IHostApplicationLifetime lifetime,
    IOptions<ChronicleOptions> options,
    IEnumerable<ISingleWriterLease> leases,
    ILogger<KernelStartupState> logger)
{
    readonly ISingleWriterLease? _lease = leases.FirstOrDefault();
    int _standbyExpiredLogged;

    /// <summary>
    /// Gets the startup status, read once so a caller never mixes two lifecycle transitions.
    /// </summary>
    internal KernelStartupStatus Status
    {
        get
        {
            if (lifetime.ApplicationStopping.IsCancellationRequested) return KernelStartupStatus.NotReady;
            if (lifetime.ApplicationStarted.IsCancellationRequested) return KernelStartupStatus.Ready;

            var lease = options.Value.Clustering.SingleWriterLease;
            if (!lease.Standby || _lease is null) return KernelStartupStatus.NotReady;
            if (_lease.IsWaiting) return KernelStartupStatus.Standby;
            if (!_lease.IsHeld) return KernelStartupStatus.NotReady;

            var heldFor = _lease.HeldFor;
            if (heldFor < lease.StandbyStartupTimeout) return KernelStartupStatus.Standby;

            if (Interlocked.Exchange(ref _standbyExpiredLogged, 1) == 0)
            {
                logger.StandbyStartupTimedOut(lease.StandbyStartupTimeout, heldFor);
            }

            return KernelStartupStatus.StandbyExpired;
        }
    }

    /// <summary>
    /// Gets whether startup has completed and shutdown has not begun.
    /// </summary>
    internal bool IsReady => Status == KernelStartupStatus.Ready;

    /// <summary>
    /// Gets whether the kernel reports standby: standby is enabled and the kernel is waiting for the single-writer
    /// lease, or has held it for less than the standby startup timeout without finishing starting. It ends once the
    /// kernel is ready, stops, loses the lease, or exceeds the timeout.
    /// </summary>
    internal bool IsStandby => Status == KernelStartupStatus.Standby;
}
