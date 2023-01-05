// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Extension methods for configuring the Cratis Kernel.
/// </summary>
public static class ClientServiceProviderExtensions
{
    /// <summary>
    /// Add the Cratis Kernel client.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisClient(this IServiceCollection services)
    {
        services.AddSingleton<IClient>(_ =>
        {
            var configuration = _.GetService<ClientConfiguration>()!;
            var httpClientFactory = _.GetService<IHttpClientFactory>()!;
            var serializerOptions = _.GetService<JsonSerializerOptions>()!;

            return configuration.ClusterType switch
            {
                ClusterType.Single => new SingleKernelClient(httpClientFactory, configuration.GetSingleKernelOptions(), serializerOptions),
                _ => throw new UnknownClusterType()
            };
        });

        return services;
    }

    /// <summary>
    /// Adds a inside silo Kernel client.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisInsideSiloClient(this IServiceCollection services)
    {
        services.AddSingleton<IClient, InsideSiloClient>();
        return services;
    }
}
