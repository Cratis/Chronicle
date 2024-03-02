// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for connectivity to the Kernel.
/// </summary>
public class KernelConnectivity
{
    /// <summary>
    /// Gets or sets the <see cref="SingleKernel"/> to use.
    /// </summary>
    public SingleKernelOptions? SingleKernel { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="StaticCluster"/> to use.
    /// </summary>
    public StaticClusterOptions? StaticCluster { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AzureStorageCluster"/> to use.
    /// </summary>
    public AzureStorageClusterOptions? AzureStorageCluster { get; set; }

    /// <summary>
    /// Gets the advertised client endpoint.
    /// </summary>
    /// <remarks>
    /// If this endpoint is not explicitly configured, it will attempt to resolve it based on the ASP.NET Core configuration and
    /// current running solution.
    /// </remarks>
    public Uri? AdvertisedClientEndpoint { get; set; }
}
