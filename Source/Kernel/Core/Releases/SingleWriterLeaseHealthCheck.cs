// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Reports readiness only while this process holds exclusive access to storage.
/// </summary>
/// <param name="lease">The process lease.</param>
public class SingleWriterLeaseHealthCheck(ISingleWriterLease lease) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(lease.IsHeld ? HealthCheckResult.Healthy("Single-writer lease held") : HealthCheckResult.Unhealthy("Single-writer lease not held"));
}
