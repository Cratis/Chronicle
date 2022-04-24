// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Aksio.Cratis.Extensions.Orleans.Configuration;
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

                case ClusterTypes.Static:
                    var staticClusterOptions = clusterConfig.GetStaticClusterOptions();
                    builder.UseDevelopmentClustering(new IPEndPoint(IPAddress.Parse(staticClusterOptions.PrimarySiloIP), staticClusterOptions.PrimarySiloPort));
                    break;

                case ClusterTypes.AdoNet:
                    {
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
                        var azureOptions = clusterConfig.GetAzureStorageClusteringOptions();
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
