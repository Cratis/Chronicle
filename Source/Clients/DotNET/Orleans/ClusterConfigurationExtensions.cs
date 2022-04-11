// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Configuration;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Orleans;

/// <summary>
/// Extension methods for working with <see cref="Cluster"/> configuration.
/// </summary>
public static class ClusterConfigurationExtensions
{
    /// <summary>
    /// Use cluster from configuration.
    /// </summary>
    /// <param name="builder"><see cref="IClientBuilder"/> to extend.</param>
    /// <param name="clusterConfig">The <see cref="Cluster"/> config.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> identifying the microservice.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <returns>Builder for continuation.</returns>
    public static IClientBuilder UseCluster(this IClientBuilder builder, Cluster clusterConfig, MicroserviceId microserviceId, ILogger? logger)
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
                logger?.UsingLocalHostClustering();
                builder.UseLocalhostClustering();
                break;

            case ClusterTypes.Static:
                {
                    logger?.UsingStaticClustering();
                    var staticClusterOptions = clusterConfig.GetStaticClusterOptions();
                    var endPoints = staticClusterOptions.Gateways.Select(_ => new IPEndPoint(IPAddress.Parse(_.Address), _.Port)).ToArray();
                    builder.UseStaticClustering(endPoints);
                }
                break;

            case ClusterTypes.AdoNet:
                {
                    logger?.UsingAdoNetClustering();
                    var adoNetOptions = clusterConfig.GetAdoNetClusteringSiloOptions();
                    builder.UseAdoNetClustering(options =>
                    {
                        options.ConnectionString = adoNetOptions.ConnectionString;
                        options.Invariant = adoNetOptions.Invariant;
                    });
                }
                break;

            case ClusterTypes.AzureStorage:
                {
                    logger?.UsingAzureStorageClustering();
                    var azureOptions = clusterConfig.GetAzureStorageClusteringOptions();
                    builder.UseAzureStorageClustering(options =>
                    {
                        options.ConfigureTableServiceClient(azureOptions.ConnectionString);
                        options.TableName = azureOptions.TableName;
                    });
                }
                break;
        }
        return builder;
    }
}
