// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for connectivity to the Kernel.
/// </summary>
public class KernelConnectivity
{
    /// <summary>
    /// Gets the <see cref="ClusterTypes"/> to use.
    /// </summary>
    public string Type { get; init; } = ClusterTypes.Single;

    /// <summary>
    /// Gets all the servers that make up the cluster to connect to.
    /// </summary>
    [ConfigurationValueResolver(typeof(ClusterOptionsValueResolver))]
    public object Options { get; init; } = new SingleKernelOptions();
}
