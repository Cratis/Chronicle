// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Reports recorded storage authorization failures without removing the kernel from rotation.
/// </summary>
/// <param name="diagnostics">The storage authorization diagnostics.</param>
public class StorageAuthorizationHealthCheck(IStorageAuthorizationDiagnostics diagnostics) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failures = diagnostics.Failures;
        if (failures.Count == 0) return Task.FromResult(HealthCheckResult.Healthy());

        var description = "Storage refused: " + string.Join("; ", failures.Select(_ => $"{_.Operation} on {_.Database}.{_.Collection}"))
            + ". See https://www.cratis.io/chronicle/hosting/configuration/mongodb-permissions/#troubleshooting-authorization-errors";
        var data = failures.GroupBy(_ => $"{_.Database}.{_.Collection}")
            .ToDictionary(_ => _.Key, _ => (object)string.Join("; ", _.Select(failure => failure.Operation)));
        return Task.FromResult(HealthCheckResult.Degraded(description, data: data));
    }
}
