// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Resolves projection declarations against the connection registries across the cluster.
/// </summary>
public static class ProjectionDeclarations
{
    /// <summary>
    /// Records declarations on the silo holding a connection, preferring the local silo.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="localSilo">The local silo address.</param>
    /// <param name="connectionId">The optional declaring connection.</param>
    /// <param name="eventStore">The projections' event store.</param>
    /// <param name="projections">The declared projections.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Record(IGrainFactory grainFactory, SiloAddress localSilo, ConnectionId? connectionId, EventStoreName eventStore, IEnumerable<ProjectionId> projections)
    {
        if (connectionId is null || connectionId == ConnectionId.NotSet) return;

        var local = grainFactory.GetConnectedClients(localSilo);
        if (await local.IsConnected(connectionId))
        {
            await local.RecordDeclaredProjections(connectionId, eventStore, projections);
            return;
        }

        var hosts = await grainFactory.GetGrain<IManagementGrain>(0).GetHosts(onlyActive: true);
        foreach (var silo in hosts.Keys.Where(silo => !silo.Equals(localSilo)))
        {
            var clients = grainFactory.GetConnectedClients(silo);
            if (await clients.IsConnected(connectionId))
            {
                await clients.RecordDeclaredProjections(connectionId, eventStore, projections);
                return;
            }
        }
    }

    /// <summary>
    /// Checks every active silo for a live connection declaring a projection. Lookup failures propagate.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="eventStore">The projection's event store.</param>
    /// <param name="projection">The projection identifier.</param>
    /// <returns>Whether a live declaring connection exists.</returns>
    public static async Task<bool> IsDeclared(IGrainFactory grainFactory, EventStoreName eventStore, ProjectionId projection)
    {
        var hosts = await grainFactory.GetGrain<IManagementGrain>(0).GetHosts(onlyActive: true);
        var declarations = await Task.WhenAll(hosts.Keys.Select(silo => grainFactory.GetConnectedClients(silo).DeclaresProjection(eventStore, projection)));
        return declarations.Any(declared => declared);
    }
}
