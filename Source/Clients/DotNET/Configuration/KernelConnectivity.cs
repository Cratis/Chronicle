// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for connectivity to the Kernel.
/// </summary>
public class KernelConnectivity
{
    /// <summary>
    /// Gets or sets the <see cref="SingleKernelOptions"/> to use.
    /// </summary>
    public SingleKernelOptions? SingleKernelOptions { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="StaticClusterOptions"/> to use.
    /// </summary>
    public StaticClusterOptions? StaticClusterOptions { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AzureStorageClusterOptions"/> to use.
    /// </summary>
    public AzureStorageClusterOptions? AzureStorageClusterOptions { get; set; }

    /// <summary>
    /// Gets the advertised client endpoint.
    /// </summary>
    /// <remarks>
    /// If this endpoint is not explicitly configured, it will attempt to resolve it based on the ASP.NET Core configuration and
    /// current running solution.
    /// </remarks>
    public Uri? AdvertisedClientEndpoint { get; set; }
}
