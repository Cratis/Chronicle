// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// Reports readiness as degraded, with the reason, while the silo is overloaded, without removing it from service.
/// </summary>
/// <remarks>
/// Degraded keeps the silo in service discovery: the other silos of the cluster are probably as busy, and taking this
/// one out of rotation would only push its clients onto them. The report is for the people and clients that would
/// otherwise see nothing but timeouts. A silo that is recovering is still reported as degraded, with that as the reason.
/// </remarks>
/// <param name="load">The <see cref="IKernelLoad"/> to read the status from.</param>
public class KernelOverloadHealthCheck(IKernelLoad load) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = load.Status;
        if (status == KernelLoadStatus.Accepting)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        var reasons = load.Reasons;
        var description = status == KernelLoadStatus.Recovering
            ? "The kernel was overloaded and is recovering."
            : $"The kernel is overloaded: {string.Join("; ", reasons.Take(5))}";
        var data = new Dictionary<string, object> { ["status"] = status.ToString(), ["limits_crossed"] = reasons.Count };
        return Task.FromResult(HealthCheckResult.Degraded(description, data: data));
    }
}
