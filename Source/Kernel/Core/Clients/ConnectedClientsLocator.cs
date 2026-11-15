// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Finds the connection registry holding a client, wherever in the cluster it is.
/// </summary>
/// <remarks>
/// A client registers on the silo that receives its <c language="csharp">Connect</c> call, but its keep-alives and the streams of its
/// reactors and reducers reach whichever silo the load balancer picks. Looking only at the registry of the silo
/// a call landed on reported a connected client as <see cref="ClientIsNotConnected"/> and let its registration expire (#5665).
/// </remarks>
public static class ConnectedClientsLocator
{
    /// <summary>
    /// Finds the registry holding a connection, preferring the given silo.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="preferred">The silo to look at first, normally the one handling the call.</param>
    /// <param name="connectionId">The connection to find.</param>
    /// <returns>The registry holding the connection, or null when no silo does.</returns>
    public static async Task<IConnectedClients?> Find(IGrainFactory grainFactory, SiloAddress preferred, ConnectionId connectionId)
    {
        if (connectionId is null || connectionId == ConnectionId.NotSet) return null;

        var local = grainFactory.GetConnectedClients(preferred);
        if (await local.IsConnected(connectionId)) return local;

        var hosts = await grainFactory.GetGrain<IManagementGrain>(0).GetHosts(onlyActive: true);
        foreach (var silo in hosts.Keys.Where(silo => !silo.Equals(preferred)))
        {
            var clients = grainFactory.GetConnectedClients(silo);
            if (await clients.IsConnected(connectionId)) return clients;
        }

        return null;
    }
}
