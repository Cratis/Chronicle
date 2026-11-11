// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Keeps readiness closed until all hosted startup work, including migrations, has completed, except in standby.
/// </summary>
/// <param name="state">The kernel startup state.</param>
internal sealed class StartupHealthCheck(KernelStartupState state) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(state.Status switch
        {
            KernelStartupStatus.Ready => HealthCheckResult.Healthy(),
            KernelStartupStatus.Standby => SingleWriterLeaseStandby.Result("Standby: kernel startup waits for the single-writer lease."),
            KernelStartupStatus.StandbyExpired => HealthCheckResult.Unhealthy(KernelReadinessHealthCheck.StandbyExpiredDescription),
            _ => HealthCheckResult.Unhealthy("Kernel startup is incomplete or the kernel is stopping.")
        });
    }
}
