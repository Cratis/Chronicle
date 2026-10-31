// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Configures the Orleans listeners for Chronicle's cluster topology.
/// </summary>
internal static class ClusterEndpointConfiguration
{
    /// <summary>
    /// Applies the configured cluster endpoints without changing membership providers.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <param name="clustering">The clustering configuration.</param>
    internal static void Configure(ISiloBuilder silo, Clustering clustering)
    {
        var gatewayPort = clustering.GatewayEnabled ? clustering.GatewayPort : 0;
        if (clustering.Type is not (ClusteringType.MongoDB or ClusteringType.Sql))
        {
            silo.UseLocalhostClustering(clustering.SiloPort, gatewayPort, serviceId: clustering.ServiceId, clusterId: clustering.ClusterId);
        }
        else if (clustering.AdvertisedIP is { } advertisedIP)
        {
            silo.ConfigureEndpoints(IPAddress.Parse(advertisedIP), clustering.SiloPort, gatewayPort, listenOnAnyHostAddress: false);
        }
        else
        {
            silo.ConfigureEndpoints(clustering.SiloPort, gatewayPort);
        }
    }
}
