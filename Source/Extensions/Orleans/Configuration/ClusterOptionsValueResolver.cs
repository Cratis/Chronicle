// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Microsoft.Extensions.Configuration;
using Orleans.Clustering.AzureStorage;
using Orleans.Configuration;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

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
            ClusterTypes.AdoNet => new AdoNetClusteringSiloOptions(),
            ClusterTypes.AzureStorage => new AzureStorageClusteringOptions(),
            _ => null!
        };
    }
}
