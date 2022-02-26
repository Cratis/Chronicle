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
}
