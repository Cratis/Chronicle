// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;
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
    /// Configure the version of the software running.
    /// </summary>
    /// <param name="version">The string representing the version of the running process.</param>
    /// <param name="commit">The string representing commit identifier from the source code control of the running process.</param>
    /// <returns>The builder to build.</returns>
    /// <remarks>
    /// Relevant operations done by the client will be tagged with this information.
    /// Example of this is when appending events, the version will be used in the event context and stored.
    /// </remarks>
    IClientBuilder WithSoftwareVersion(string version, string commit);

    /// <summary>
    /// Configure what the running program is identified as.
    /// </summary>
    /// <param name="name">Name that identifies the running program.</param>
    /// <returns>The builder to build.</returns>
    IClientBuilder IdentifiedAs(string name);

    /// <summary>
    /// Specify the specific <see cref="IIdentityProvider"/> to use.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IIdentityProvider"/>.</typeparam>
    /// <returns>The builder to build.</returns>
    IClientBuilder UseIdentityProvider<T>() where T : IIdentityProvider;

    /// <summary>
    /// Configure any metadata to associate with the client.
    /// </summary>
    /// <param name="key">Key of the metadata.</param>
    /// <param name="value">Value belonging to the metadata.</param>
    /// <returns>The builder to build.</returns>
    /// <remarks>
    /// Relevant operations done by the client will be tagged with this information.
    /// Example of this is when appending events, the version will be used in the event context and stored.
    /// </remarks>
    IClientBuilder WithMetadata(string key, string value);

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
