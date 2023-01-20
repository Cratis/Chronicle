// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the client configuration.
/// </summary>
[Configuration]
public class ClientConfiguration
{
    /// <summary>
    /// Gets the <see cref="ClusterType"/>.
    /// </summary>
    public ClusterType ClusterType { get; init; } = ClusterType.Single;

    /// <summary>
    /// Gets all the servers that make up the cluster to connect to.
    /// </summary>
    public object Options { get; init; } = new SingleKernelOptions();

    /// <summary>
    /// Gets the advertised client endpoint.
    /// </summary>
    /// <remarks>
    /// If this endpoint is not explicitly configured, it will attempt to resolve it based on the ASP.NET Core configuration and
    /// current running solution.
    /// </remarks>
    public Uri? AdvertisedClientEndpoint { get; init; }
}
