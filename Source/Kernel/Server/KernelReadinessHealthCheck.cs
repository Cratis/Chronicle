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
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (state.IsReady) return Task.FromResult(HealthCheckResult.Healthy());
        if (state.IsStandby) return Task.FromResult(SingleWriterLeaseStandby.Result("Standby: Chronicle starts serving once it holds the single-writer lease and has started."));
        return Task.FromResult(HealthCheckResult.Unhealthy("Chronicle is not ready."));
    }
}
