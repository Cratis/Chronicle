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
        if (state.IsReady) return Task.FromResult(HealthCheckResult.Healthy());
        if (state.IsStandby) return Task.FromResult(SingleWriterLeaseStandby.Result("Standby: kernel startup waits for the single-writer lease."));
        return Task.FromResult(HealthCheckResult.Unhealthy("Kernel startup is incomplete or the kernel is stopping."));
    }
}
