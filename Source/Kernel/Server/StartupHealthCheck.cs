// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Keeps readiness closed until all hosted startup work, including migrations, has completed.
/// </summary>
/// <param name="lifetime">The host application lifetime.</param>
internal sealed class StartupHealthCheck(IHostApplicationLifetime lifetime) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(lifetime.ApplicationStarted.IsCancellationRequested && !lifetime.ApplicationStopping.IsCancellationRequested
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Kernel startup is incomplete or the kernel is stopping."));
}
