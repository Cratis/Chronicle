// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis;

/// <summary>
/// Defines a builder for building client ready for use.
/// </summary>
public interface IClientBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used for building the client.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Instruct the builder that the client is multi tenanted.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    /// <remarks>By default the client will be single tenanted.</remarks>
    IClientBuilder MultiTenanted();

    /// <summary>
    /// Configure for a specific microservice.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/>.</param>
    /// <param name="microserviceName">The <see cref="MicroserviceName"/>.</param>
    /// <returns>The builder to build.</returns>
    /// <remarks>By default the client will be for a non specific Microservice, indicating you're not building a microservice oriented system.</remarks>
    IClientBuilder ForMicroservice(MicroserviceId microserviceId, MicroserviceName microserviceName);

    /// <summary>
    /// Instruct the builder that the client is within the kernel.
    /// </summary>
    /// <returns>Builder for continuation.</returns>
    IClientBuilder InKernel();

    /// <summary>
    /// Specify what <see cref="IModelNameConvention"/> to use.
    /// </summary>
    /// <param name="convention"><see cref="IModelNameConvention"/> to use.</param>
    /// <returns>Builder for continuation.</returns>
    IClientBuilder UseModelNameConvention(IModelNameConvention convention);

    /// <summary>
    /// Specify what <see cref="IClientArtifactsProvider"/> to use.
    /// </summary>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/>.</param>
    /// <returns>Builder for continuation.</returns>
    IClientBuilder UseClientArtifacts(IClientArtifactsProvider clientArtifactsProvider);

    /// <summary>
    /// Build the client.
    /// </summary>
    void Build();
}
