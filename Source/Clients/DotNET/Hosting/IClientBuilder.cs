// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Hosting;

/// <summary>
/// Defines a builder for building client ready for use.
/// </summary>
public interface IClientBuilder
{
    /// <summary>
    /// Instruct the builder that the client is within the kernel.
    /// </summary>
    /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
    IClientBuilder InKernel();

    /// <summary>
    /// Specify what <see cref="IModelNameConvention"/> to use.
    /// </summary>
    /// <param name="convention"><see cref="IModelNameConvention"/> to use.</param>
    /// <returns><see cref="IClientBuilder"/> for continuation.</returns>
    IClientBuilder UseModelNameConvention(IModelNameConvention convention);

    /// <summary>
    /// Build the client.
    /// </summary>
    /// <param name="hostBuilderContext"><see cref="HostBuilderContext"/> we're building for.</param>
    /// <param name="services"><see cref="IServiceCollection"/> to register services for.</param>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts. Will default to <see cref="DefaultClientArtifactsProvider"/>.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/>.</param>
    void Build(HostBuilderContext hostBuilderContext, IServiceCollection services, IClientArtifactsProvider? clientArtifacts = default, ILoggerFactory? loggerFactory = default);
}
