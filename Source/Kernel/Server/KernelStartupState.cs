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
internal sealed class KernelStartupState(IHostApplicationLifetime lifetime, IOptions<ChronicleOptions> options, IEnumerable<ISingleWriterLease> leases)
{
    readonly ISingleWriterLease? _lease = leases.FirstOrDefault();

    /// <summary>
    /// Gets whether startup has completed and shutdown has not begun.
    /// </summary>
    internal bool IsReady => lifetime.ApplicationStarted.IsCancellationRequested && !lifetime.ApplicationStopping.IsCancellationRequested;

    /// <summary>
    /// Gets whether the kernel reports standby: standby is enabled and the kernel is waiting for, or has just taken,
    /// the single-writer lease and has not finished starting. It ends once the kernel is ready, stops, or loses the lease.
    /// </summary>
    internal bool IsStandby =>
        options.Value.Clustering.SingleWriterLease.Standby
        && _lease is not null
        && (_lease.IsWaiting || _lease.IsHeld)
        && !lifetime.ApplicationStarted.IsCancellationRequested
        && !lifetime.ApplicationStopping.IsCancellationRequested;
}
