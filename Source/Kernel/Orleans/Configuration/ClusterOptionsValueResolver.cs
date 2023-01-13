// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Microsoft.Extensions.Configuration;

namespace Aksio.Cratis.Kernel.Orleans.Configuration;

/// <summary>
/// Represents a <see cref="IConfigurationValueResolver"/> for resolving options on <see cref="Cluster"/>.
/// </summary>
public class ClusterOptionsValueResolver : IConfigurationValueResolver
{
    /// <inheritdoc/>
    public object Resolve(IConfiguration configuration)
    {
        return configuration.GetValue<string>("type") switch
        {
            ClusterTypes.Static => new StaticClusterOptions(),
            ClusterTypes.AdoNet => new AdoNetClusterOptions(),
            ClusterTypes.AzureStorage => new AzureStorageClusterOptions(),
            _ => null!
        };
    }
}
