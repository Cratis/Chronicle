// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the cluster configuration.
/// </summary>
[Configuration]
public class Cluster
{
    /// <summary>
    /// Gets the name of the cluster.
    /// </summary>
    public string Name { get; init; } = "Cratis";

    /// <summary>
    /// Gets the type of cluster to use.
    /// </summary>
    public string Type { get; init; } = ClusterTypes.Local;

    /// <summary>
    /// Gets the host name for the silo. If this is specified, it will not use the AdvertisedIP for the membership table.
    /// </summary>
    public string SiloHostName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the advertised IP address to be used in the Orleans membership table.
    /// </summary>
    public string AdvertisedIP { get; init; } = "127.0.0.1";

    /// <summary>
    /// Gets the port used for the Silo in the cluster.
    /// </summary>
    public int SiloPort { get; init; } = 11111;

    /// <summary>
    /// Gets the port used to connect as a gateway (Clients).
    /// </summary>
    public int GatewayPort { get; init; } = 30000;

    /// <summary>
    /// Gets the options in the form of a JSON representation for the cluster configuration.
    /// </summary>
    [ConfigurationValueResolver(typeof(ClusterOptionsValueResolver))]
    public object Options { get; init; } = null!;
}
