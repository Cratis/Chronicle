// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Aksio.Cratis.Kernel.Orleans.Configuration;
using Aksio.Cratis.Kernel.Server;
using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for working with <see cref="Cluster"/> configuration.
/// </summary>
public static class ClusterConfigurationExtensions
{
    /// <summary>
    /// Use cluster from configuration.
    /// </summary>
    /// <param name="builder"><see cref="ISiloBuilder"/> to extend.</param>
    /// <returns>Builder for continuation.</returns>
    public static ISiloBuilder UseCluster(this ISiloBuilder builder)
    {
        builder.ConfigureServices(_ =>
        {
            var clusterConfig = _.GetClusterConfig();

            var logger = _.BuildServiceProvider().GetService<ILogger<Startup>>();

            builder
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = clusterConfig.Name;
                    options.ServiceId = "kernel";
                });

            if (!string.IsNullOrEmpty(clusterConfig.AdvertisedIP))
            {
                logger?.UsingAdvertisedIP(clusterConfig.AdvertisedIP);

                builder.ConfigureEndpoints(
                    advertisedIP: IPAddress.Parse(clusterConfig.AdvertisedIP),
                    siloPort: clusterConfig.SiloPort,
                    gatewayPort: clusterConfig.GatewayPort,
                    listenOnAnyHostAddress: true);
            }
            else
            {
                if (clusterConfig.SiloHostName is not null)
                {
                    logger?.UsingSiloHostName(clusterConfig.SiloHostName);
                }
                else
                {
                    logger?.UsingResolvedHostName(Dns.GetHostName());
                }

                builder.ConfigureEndpoints(
                    hostname: !string.IsNullOrEmpty(clusterConfig.SiloHostName) ? clusterConfig.SiloHostName : Dns.GetHostName(),
                    siloPort: clusterConfig.SiloPort,
                    gatewayPort: clusterConfig.GatewayPort,
                    listenOnAnyHostAddress: true);
            }

            switch (clusterConfig.Type)
            {
                case ClusterTypes.Single:
                    logger?.UsingLocalHostClustering();
                    builder.UseLocalhostClustering();
                    break;

                case ClusterTypes.Static:
                    logger?.UsingStaticHostClustering();
                    var staticClusterOptions = clusterConfig.GetStaticClusterOptions();
                    builder.UseDevelopmentClustering(new IPEndPoint(IPAddress.Parse(staticClusterOptions.PrimarySiloIP), staticClusterOptions.PrimarySiloPort));
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
        });
        return builder;
    }
}
