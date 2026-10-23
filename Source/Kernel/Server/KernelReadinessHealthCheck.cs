// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Reports whether the kernel has completed startup and is not stopping.
/// </summary>
/// <param name="state">The kernel startup state.</param>
internal sealed class KernelReadinessHealthCheck(KernelStartupState state) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Chronicle is not ready."));
}
