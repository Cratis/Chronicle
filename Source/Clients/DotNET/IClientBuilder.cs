// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis;

/// <summary>
/// Defines a builder for building client ready for use.
/// </summary>
/// <typeparam name="TActual">Type of the actual builder.</typeparam>
/// <typeparam name="TClient">Type of client.</typeparam>
public interface IClientBuilder<TActual, TClient>
    where TActual : class, IClientBuilder<TActual, TClient>
{
    /// <summary>
    /// Configure for a specific microservice.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/>.</param>
    /// <param name="microserviceName">The <see cref="MicroserviceName"/>.</param>
    /// <returns>The builder to build.</returns>
    TActual ForMicroservice(MicroserviceId microserviceId, MicroserviceName microserviceName);

    /// <summary>
    /// Instruct the builder that the client is within the kernel.
    /// </summary>
    /// <returns>Actual builder for continuation.</returns>
    TActual InKernel();

    /// <summary>
    /// Specify what <see cref="IModelNameConvention"/> to use.
    /// </summary>
    /// <param name="convention"><see cref="IModelNameConvention"/> to use.</param>
    /// <returns>Actual builder for continuation.</returns>
    TActual UseModelNameConvention(IModelNameConvention convention);

    /// <summary>
    /// Build the client.
    /// </summary>
    /// <param name="services">Optional <see cref="IServiceCollection"/> to register services for. Will create its own service collection if none is specified.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts. Will default to <see cref="DefaultClientArtifactsProvider"/>.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    TClient Build(IServiceCollection? services = default, IClientArtifactsProvider? clientArtifacts = default, ILoggerFactory? loggerFactory = default);
}
