// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Reports MongoDB health from the driver's monitored cluster state without using a pool connection.
/// </summary>
/// <param name="clientManager">The manager of shared MongoDB clients.</param>
/// <param name="settings">The MongoDB client settings.</param>
public class MongoDBClusterHealthCheck(IMongoDBClientManager clientManager, MongoClientSettings settings) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var description = clientManager.GetClientFor(settings).Cluster.Description;
        var connected = description.Servers.Where(_ => _.State == ServerState.Connected).ToArray();
        if (connected.Any(_ => _.Type is ServerType.ReplicaSetPrimary or ServerType.Standalone or ServerType.ShardRouter or ServerType.LoadBalanced))
        {
            return Task.FromResult(HealthCheckResult.Healthy("A writable MongoDB server is connected."));
        }

        if (connected.Length > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded("MongoDB servers are connected, but no writable server is available."));
        }

        var failures = description.Servers.Select(_ => _.HeartbeatException).Where(_ => _ is not null);
        return Task.FromResult(HealthCheckResult.Unhealthy($"No MongoDB server is connected. {string.Join("; ", failures)}"));
    }
}
