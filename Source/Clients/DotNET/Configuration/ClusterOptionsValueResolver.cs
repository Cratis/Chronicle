// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents a <see cref="IConfigurationValueResolver"/> for resolving options on <see cref="KernelConnectivity"/>.
/// </summary>
public class ClusterOptionsValueResolver : IConfigurationValueResolver
{
    /// <inheritdoc/>
    public object Resolve(IConfiguration configuration)
    {
        return configuration.GetValue<string>("type") switch
        {
            ClusterTypes.Single => new SingleKernelOptions(),
            ClusterTypes.Static => new StaticClusterOptions(),
            ClusterTypes.AzureStorage => new AzureStorageClusterOptions(),
            _ => null!
        };
    }
}
