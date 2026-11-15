// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Reports whether the kernel has completed startup and is not stopping, or is in standby.
/// </summary>
/// <param name="state">The kernel startup state.</param>
internal sealed class KernelReadinessHealthCheck(KernelStartupState state) : IHealthCheck
{
    /// <summary>
    /// The description reported when startup did not complete within the standby startup timeout.
    /// </summary>
    internal const string StandbyExpiredDescription = "Chronicle did not finish starting within Clustering.SingleWriterLease.StandbyStartupTimeout after taking the single-writer lease.";

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(state.Status switch
        {
            KernelStartupStatus.Ready => HealthCheckResult.Healthy(),
            KernelStartupStatus.Standby => SingleWriterLeaseStandby.Result("Standby: Chronicle starts serving once it holds the single-writer lease and has started."),
            KernelStartupStatus.StandbyExpired => HealthCheckResult.Unhealthy(StandbyExpiredDescription),
            _ => HealthCheckResult.Unhealthy("Chronicle is not ready.")
        });
    }
}
