// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Extension methods for working with <see cref="Cluster"/> config.
/// </summary>
public static class ClusterConfigurationExtensions
{
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
    public static StaticClusterOptions GetStaticClusterOptions(this Cluster clusterConfig) => clusterConfig.Options as StaticClusterOptions ?? new StaticClusterOptions();

    /// <summary>
    /// Get specific <see cref="AdoNetClusterOptions"/> from the options of <see cref="Cluster"/>.
    /// </summary>
    /// <param name="clusterConfig"><see cref="Cluster"/> to get from.</param>
    /// <returns><see cref="AdoNetClusterOptions"/> instance.</returns>
    public static AdoNetClusterOptions GetAdoNetClusterOptions(this Cluster clusterConfig) => clusterConfig.Options as AdoNetClusterOptions ?? new AdoNetClusterOptions();

    /// <summary>
    /// Get specific <see cref="AzureStorageClusterOptions"/> from the options of <see cref="Cluster"/>.
    /// </summary>
    /// <param name="clusterConfig"><see cref="Cluster"/> to get from.</param>
    /// <returns><see cref="AzureStorageClusterOptions"/> instance.</returns>
    public static AzureStorageClusterOptions GetAzureStorageClusterOptions(this Cluster clusterConfig) => clusterConfig.Options as AzureStorageClusterOptions ?? new AzureStorageClusterOptions();
}
