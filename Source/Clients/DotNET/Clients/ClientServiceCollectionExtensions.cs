// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Configuration;
using Aksio.Cratis.Configuration;
using Aksio.Net;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Extension methods for configuring the Cratis Kernel.
/// </summary>
public static class ClientServiceCollectionExtensions
{
    /// <summary>
    /// Add the Cratis Kernel client.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisClient(this IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var httpClientFactory = _.GetRequiredService<IHttpClientFactory>();
            var configuration = _.GetService<ClientConfiguration>() ?? new ClientConfiguration();
            var loadBalancer = _.GetService<ILoadBalancer>() ?? new LoadBalancer(httpClientFactory);
            var serializerOptions = _.GetRequiredService<JsonSerializerOptions>();
            serializerOptions = new JsonSerializerOptions(serializerOptions);
            var orleansAzureTableClientLogger = _.GetRequiredService<ILogger<OrleansAzureTableStoreKernelClient>>();
            var logger = _.GetRequiredService<ILogger<SingleKernelClient>>();
            var server = _.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>();
            var clientLifecycle = _.GetRequiredService<IClientLifecycle>();
            var executionContextManager = _.GetRequiredService<IExecutionContextManager>();
            var taskFactory = _.GetRequiredService<ITaskFactory>();
            var timerFactory = _.GetRequiredService<ITimerFactory>();

            var loadBalancedHttpClientFactory = loadBalancer.CreateHttpClientFactory(new RoundRobinLoadBalancerStrategy());
            if (configuration.Kernel.AdvertisedClientEndpoint is null && addresses!.Addresses.Count == 0)
            {
                throw new UnableToResolveClientUri();
            }

            var clientEndpoint = configuration.Kernel.AdvertisedClientEndpoint ?? addresses!.GetFirstAddressAsUri();
            IClient client = configuration.Kernel.Type switch
            {
                ClusterTypes.Single => new SingleKernelClient(
                    httpClientFactory,
                    configuration.Kernel.GetSingleKernelOptions(),
                    taskFactory,
                    timerFactory,
                    executionContextManager,
                    clientEndpoint,
                    clientLifecycle,
                    serializerOptions,
                    logger),

                ClusterTypes.Static => new StaticClusteredKernelClient(
                    loadBalancedHttpClientFactory,
                    configuration.Kernel.GetStaticClusterOptions(),
                    taskFactory,
                    timerFactory,
                    executionContextManager,
                    clientEndpoint,
                    clientLifecycle,
                    serializerOptions,
                    logger),

                ClusterTypes.AzureStorage => new OrleansAzureTableStoreKernelClient(
                    loadBalancedHttpClientFactory,
                    configuration.Kernel.GetAzureStorageClusterOptions(),
                    taskFactory,
                    timerFactory,
                    executionContextManager,
                    clientEndpoint,
                    clientLifecycle,
                    serializerOptions,
                    orleansAzureTableClientLogger,
                    logger),

                _ => throw new UnknownClusterType()
            };
            logger.ConnectingToKernel();

            LogClientType(configuration, logger);

            client.Connect().Wait();

            return client;
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
        services.AddSingleton<IClient>(_ =>
        {
            var server = _.GetRequiredService<IServer>();
            var httpClientFactory = _.GetRequiredService<IHttpClientFactory>();
            var taskFactory = _.GetRequiredService<ITaskFactory>();
            var timerFactory = _.GetRequiredService<ITimerFactory>();
            var executionContextManager = _.GetRequiredService<IExecutionContextManager>();
            var clientLifecycle = _.GetRequiredService<IClientLifecycle>();
            var serializerOptions = _.GetRequiredService<JsonSerializerOptions>();
            serializerOptions = new JsonSerializerOptions(serializerOptions);
            var singleKernelClientLogger = _.GetRequiredService<ILogger<SingleKernelClient>>();
            var logger = _.GetRequiredService<ILogger<InsideKernelClient>>();

            var client = new InsideKernelClient(
                server,
                httpClientFactory,
                taskFactory,
                timerFactory,
                executionContextManager,
                clientLifecycle,
                serializerOptions,
                singleKernelClientLogger,
                logger);

            singleKernelClientLogger.ConnectingToKernel();
            client.Connect().Wait();

            return client;
        });
        return services;
    }

    static void LogClientType(ClientConfiguration configuration, ILogger<SingleKernelClient> logger)
    {
        switch (configuration.Kernel.Type)
        {
            case ClusterTypes.Single: logger.UsingSingleKernelClient(); break;
            case ClusterTypes.Static: logger.UsingStaticClusterKernelClient(); break;
            case ClusterTypes.AzureStorage: logger.UsingOrleansAzureStorageKernelClient(); break;
        }
    }
}
