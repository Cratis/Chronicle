// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Reports deferred registrations without removing a serving kernel from service discovery.
/// </summary>
/// <param name="retries">The deferred registrations.</param>
public class KernelProjectionsRegistrationHealthCheck(IKernelProjectionRegistrationRetries retries) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var pending = retries.Pending;
        return Task.FromResult(pending.Count == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"Kernel projections are not registered yet for event stores: {string.Join(", ", pending.Select(_ => $"{_} (background attempts: {retries.GetAttemptCount(_)})"))}. Retrying in the background."));
    }
}
