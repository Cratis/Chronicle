// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Configuration;
using Aksio.Cratis.Network;
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
    public static ISiloHostBuilder UseCluster(this ISiloHostBuilder builder, Cluster clusterConfig, MicroserviceId microserviceId, ILogger? logger)
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

                var siloPort = IpUtilities.GetAvailablePort(11112, 12000) ?? 0;
                var gatewayPort = IpUtilities.GetAvailablePort(30001, 31000) ?? 0;

                builder.ConfigureEndpoints(advertisedIP: IPAddress.Parse("127.0.0.1"), siloPort, gatewayPort, listenOnAnyHostAddress: true);
                builder.UseDevelopmentClustering(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11111));
                break;

            case ClusterTypes.Static:
                {
                    logger?.UsingStaticClustering();
                    var staticClusterOptions = clusterConfig.GetStaticClusterOptions();
                    var endPoints = staticClusterOptions.Gateways.Select(_ =>
                    {
                        var hostEntry = Dns.GetHostEntry(_.Address);
                        var ipAddress = hostEntry.AddressList.FirstOrDefault();
                        return new IPEndPoint(ipAddress!, _.Port);
                    }).ToArray();

                    // builder.UseStaticClustering(endPoints);
                }
                break;

            case ClusterTypes.AdoNet:
                {
                    logger?.UsingAdoNetClustering();
                    var adoNetOptions = clusterConfig.GetAdoNetClusterOptions();
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
                    var azureOptions = clusterConfig.GetAzureStorageClusterOptions();
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
