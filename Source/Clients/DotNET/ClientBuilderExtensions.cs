// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Configuration;
using Aksio.Cratis.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis;

/// <summary>
/// Extension methods for configuring the client.
/// </summary>
public static class ClientBuilderExtensions
{
    /// <inheritdoc/>
    public static IClientBuilder UseWithSingleKernel(this IClientBuilder builder, Action<SingleKernelOptions>? configure)
    {
        builder.Services.Configure<ClientConfiguration>(_ =>
        {
            var options = _.Kernel.SingleKernelOptions ?? new SingleKernelOptions();
            configure?.Invoke(options);
        });

        return builder;
    }

    /// <inheritdoc/>
    public static IClientBuilder UseWithStaticCluster(this IClientBuilder builder, Action<StaticClusterOptions>? configure)
    {
        builder.Services.Configure<ClientConfiguration>(_ =>
        {
            var options = _.Kernel.StaticClusterOptions ?? new StaticClusterOptions();
            configure?.Invoke(options);
        });

        return builder;
    }

    /// <inheritdoc/>
    public static IClientBuilder UseWithAzureStorageCluster(this IClientBuilder builder, Action<AzureStorageClusterOptions>? configure)
    {
        builder.Services.Configure<ClientConfiguration>(_ =>
        {
            var options = _.Kernel.AzureStorageClusterOptions ?? new AzureStorageClusterOptions();
            configure?.Invoke(options);
        });

        return builder;
    }
}
