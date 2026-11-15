// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Reports readiness while this process holds exclusive access to storage, or standby while it waits for it
/// when standby is enabled.
/// </summary>
/// <param name="lease">The process lease.</param>
/// <param name="options">The Chronicle configuration.</param>
public class SingleWriterLeaseHealthCheck(ISingleWriterLease lease, IOptions<ChronicleOptions> options) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (lease.IsHeld) return Task.FromResult(HealthCheckResult.Healthy("Single-writer lease held"));
        if (lease.IsWaiting && options.Value.Clustering.SingleWriterLease.Standby)
        {
            return Task.FromResult(SingleWriterLeaseStandby.Result("Standby: waiting for the single-writer lease"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Single-writer lease not held"));
    }
}
