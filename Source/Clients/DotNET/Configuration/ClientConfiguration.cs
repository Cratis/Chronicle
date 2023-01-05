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
    public ClusterType ClusterType { get; init; } = ClusterType.Static;

    /// <summary>
    /// Gets all the servers that make up the cluster to connect to.
    /// </summary>
    public object Options { get; init; } = new SingleKernelOptions();
}

