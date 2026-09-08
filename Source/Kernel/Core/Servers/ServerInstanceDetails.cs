// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Reactive;

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Represents the read model for a running server instance in the cluster.
/// </summary>
/// <param name="Id">The unique identifier of the server instance.</param>
/// <param name="Address">The address of the server instance.</param>
/// <param name="Status">The cluster membership status of the server instance.</param>
/// <param name="CpuUsagePercentage">The percentage of available CPU capacity the process used over the last sampling window.</param>
/// <param name="MemoryUsageBytes">The process' working set, in bytes.</param>
[ReadModel]
public record ServerInstanceDetails(
    string Id,
    string Address,
    string Status,
    double CpuUsagePercentage,
    long MemoryUsageBytes)
{
    /// <summary>
    /// Gets every running server instance in the cluster, with a live snapshot of its CPU and memory usage.
    /// </summary>
    /// <param name="servers">The <see cref="ServerInstancesQuery"/> holding the cluster view.</param>
    /// <returns>A collection of <see cref="ServerInstanceDetails"/>.</returns>
    internal static async Task<IEnumerable<ServerInstanceDetails>> GetServerInstances(ServerInstancesQuery servers) =>
        await servers.GetAll();

    /// <summary>
    /// Observes every running server instance in the cluster, polling for fresh CPU and memory snapshots.
    /// </summary>
    /// <param name="servers">The <see cref="ServerInstancesQuery"/> holding the cluster view.</param>
    /// <returns>An observable subject emitting collections of <see cref="ServerInstanceDetails"/>.</returns>
    internal static ISubject<IEnumerable<ServerInstanceDetails>> AllServerInstances(ServerInstancesQuery servers) =>
        servers.InvokeAndWrapWithSubject(servers.ObserveAll);
}
