// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis;
using Aksio.Cratis.Extensions.Orleans.Configuration;
using Orleans.Clustering.AzureStorage;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Orleans;

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
    /// <param name="builder"><see cref="IClientBuilder"/> to extend.</param>
    /// <param name="clusterConfig">The <see cref="Cluster"/> config.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> identifying the microservice.</param>
    /// <returns>Builder for continuation.</returns>
    public static IClientBuilder UseCluster(this IClientBuilder builder, Cluster clusterConfig, MicroserviceId microserviceId)
    {
        builder
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = clusterConfig.Name;
                options.ServiceId = microserviceId.ToString();
            });

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
        return builder;
    }
}
