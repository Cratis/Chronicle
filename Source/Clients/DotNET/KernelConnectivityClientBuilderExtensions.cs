// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis;

/// <summary>
/// Extension methods for configuring the client.
/// </summary>
public static class KernelConnectivityClientBuilderExtensions
{
    /// <summary>
    /// Configure the client to use a single kernel.
    /// </summary>
    /// <param name="builder">Builder to configure.</param>
    /// <param name="configure">Callback for configuring the options.</param>
    /// <returns>The builder for continuation.</returns>
    public static IClientBuilder UseWithSingleKernel(this IClientBuilder builder, Action<SingleKernelOptions>? configure)
    {
        builder.Services.Configure<ClientOptions>(_ =>
        {
            var options = _.Kernel.SingleKernel ?? new SingleKernelOptions();
            configure?.Invoke(options);
        });

        return builder;
    }

    /// <summary>
    /// Configure the client to use a static cluster.
    /// </summary>
    /// <param name="builder">Builder to configure.</param>
    /// <param name="configure">Callback for configuring the options.</param>
    /// <returns>The builder for continuation.</returns>
    public static IClientBuilder UseWithStaticCluster(this IClientBuilder builder, Action<StaticClusterOptions>? configure)
    {
        builder.Services.Configure<ClientOptions>(_ =>
        {
            var options = _.Kernel.StaticCluster ?? new StaticClusterOptions
            {
                Endpoints = Enumerable.Empty<Uri>()
            };
            configure?.Invoke(options);
        });

        return builder;
    }

    /// <summary>
    /// Configure the client to use a Azure Storage account as source for the cluster, Kernel must be configured with the same option for this to work.
    /// </summary>
    /// <param name="builder">Builder to configure.</param>
    /// <param name="configure">Callback for configuring the options.</param>
    /// <returns>The builder for continuation.</returns>
    public static IClientBuilder UseWithAzureStorageCluster(this IClientBuilder builder, Action<AzureStorageClusterOptions>? configure)
    {
        builder.Services.Configure<ClientOptions>(_ =>
        {
            var options = _.Kernel.AzureStorageCluster ?? new AzureStorageClusterOptions
            {
                TableName = AzureStorageClusterOptions.DEFAULT_TABLE_NAME,
                ConnectionString = string.Empty,
                Port = 80,
                Secure = false
            };

            configure?.Invoke(options);
        });

        return builder;
    }
}
