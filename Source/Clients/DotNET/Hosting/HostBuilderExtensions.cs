// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis;
using Aksio.Cratis.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for using Aksio.Cratis in an application.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="IClientBuilder"/> for a non-microservice oriented scenario.
    /// </summary>
    /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
    /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts. Will default to <see cref="DefaultClientArtifactsProvider"/>.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
    public static IHostBuilder UseCratis(
        this IHostBuilder hostBuilder,
        Action<IClientBuilder>? configureDelegate = default,
        IClientArtifactsProvider? clientArtifacts = default,
        ILoggerFactory? loggerFactory = default)
    {
        return hostBuilder.UseCratis(MicroserviceId.Unspecified, MicroserviceName.Unspecified, configureDelegate, clientArtifacts, loggerFactory);
    }

    /// <summary>
    /// Configures the <see cref="IClientBuilder"/> for a microservice oriented scenario.
    /// </summary>
    /// <param name="hostBuilder"><see cref="IHostBuilder"/> to build on.</param>
    /// <param name="microserviceId">The unique <see cref="MicroserviceId"/> for the microservice.</param>
    /// <param name="microserviceName">The <see cref="MicroserviceName"/> for the microservice.</param>
    /// <param name="configureDelegate">Optional delegate used to configure the Cratis client.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts. Will default to <see cref="DefaultClientArtifactsProvider"/>.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    /// <returns><see cref="IHostBuilder"/> for configuration continuation.</returns>
    public static IHostBuilder UseCratis(
        this IHostBuilder hostBuilder,
        MicroserviceId microserviceId,
        MicroserviceName microserviceName,
        Action<IClientBuilder>? configureDelegate = default,
        IClientArtifactsProvider? clientArtifacts = default,
        ILoggerFactory? loggerFactory = default)
    {
        var clientBuilder = ClientBuilder.ForMicroservice(microserviceId, microserviceName);
        configureDelegate?.Invoke(clientBuilder);
        hostBuilder.ConfigureServices((context, services) => clientBuilder.Build(context, services, clientArtifacts, loggerFactory));
        return hostBuilder;
    }
}
