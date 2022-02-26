// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Clustering.AzureStorage;
using Orleans.Configuration;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Extension methods for working with <see cref="Cluster"/> config.
/// </summary>
public static class ClusterConfigurationExtensions
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Get configured cluster config from the configured services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to get from.</param>
    /// <returns><see cref="Cluster"/> config.</returns>
    /// <remarks>
    /// If the config is not configured, a default setup for localhost will be returned.
    /// </remarks>
    public static Cluster GetClusterConfig(this IServiceCollection services) => services.FirstOrDefault(service => service.ServiceType == typeof(Cluster))?.ImplementationInstance as Cluster ?? new Cluster();

    /// <summary>
    /// Get specific <see cref="StaticClusterOptions"/> from the options of <see cref="Cluster"/>.
    /// </summary>
    /// <param name="clusterConfig"><see cref="Cluster"/> to get from.</param>
    /// <returns><see cref="StaticClusterOptions"/> instance.</returns>
    public static StaticClusterOptions GetStaticClusterOptions(this Cluster clusterConfig) => JsonSerializer.Deserialize<StaticClusterOptions>(
                            JsonSerializer.Serialize(clusterConfig.Options), _jsonOptions)!;

    /// <summary>
    /// Get specific <see cref="AdoNetClusteringSiloOptions"/> from the options of <see cref="Cluster"/>.
    /// </summary>
    /// <param name="clusterConfig"><see cref="Cluster"/> to get from.</param>
    /// <returns><see cref="AdoNetClusteringSiloOptions"/> instance.</returns>
    public static AdoNetClusteringSiloOptions GetAdoNetClusteringSiloOptions(this Cluster clusterConfig) => JsonSerializer.Deserialize<AdoNetClusteringSiloOptions>(
                            JsonSerializer.Serialize(clusterConfig.Options), _jsonOptions)!;

    /// <summary>
    /// Get specific <see cref="AzureStorageClusteringOptions"/> from the options of <see cref="Cluster"/>.
    /// </summary>
    /// <param name="clusterConfig"><see cref="Cluster"/> to get from.</param>
    /// <returns><see cref="AzureStorageClusteringOptions"/> instance.</returns>
    public static AzureStorageClusteringOptions GetAzureStorageClusteringOptions(this Cluster clusterConfig) => JsonSerializer.Deserialize<AzureStorageClusteringOptions>(
                            JsonSerializer.Serialize(clusterConfig.Options), _jsonOptions)!;
}
