// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Aksio.Cratis.Extensions.Orleans.Configuration;
using Orleans.Clustering.AzureStorage;
using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for working with <see cref="Cluster"/> configuration.
/// </summary>
public static class ClusterConfigurationExtensions
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Use cluster from configuration.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to extend.</param>
    /// <returns>Builder for continuation.</returns>
    public static ISiloBuilder UseCluster(this ISiloBuilder builder)
    {
        builder.ConfigureServices(_ =>
        {
            var clusterConfig = _.FirstOrDefault(service => service.ServiceType == typeof(Cluster))?.ImplementationInstance as Cluster ?? new Cluster();

            builder
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterConfig.Name;
                    options.ServiceId = "kernel";
                })
                .ConfigureEndpoints(
                    advertisedIP: IPAddress.Parse(clusterConfig.AdvertisedIP),
                    siloPort: clusterConfig.SiloPort,
                    gatewayPort: clusterConfig.GatewayPort,
                    listenOnAnyHostAddress: true);

            switch (clusterConfig.Type)
            {
                case ClusterTypes.Local:
                    builder.UseLocalhostClustering();
                    break;

                case ClusterTypes.AdoNet:
                    {
                        var parsedOptions = JsonSerializer.Deserialize<AdoNetClusteringSiloOptions>(
                                JsonSerializer.Serialize(clusterConfig.Options), _jsonOptions)!;
                        builder.UseAdoNetClustering(options =>
                        {
                            options.ConnectionString = parsedOptions.ConnectionString;
                            options.Invariant = parsedOptions.Invariant;
                        });
                    }
                    break;

                case ClusterTypes.AzureStorage:
                    {
                        var parsedOptions = JsonSerializer.Deserialize<AzureStorageClusteringOptions>(
                                JsonSerializer.Serialize(clusterConfig.Options), _jsonOptions)!;
                        builder.UseAzureStorageClustering(options =>
                        {
                            options.ConnectionString = parsedOptions.ConnectionString;
                            options.TableName = parsedOptions.TableName;
                        });
                    }
                    break;
            }
        });
        return builder;
    }
}
