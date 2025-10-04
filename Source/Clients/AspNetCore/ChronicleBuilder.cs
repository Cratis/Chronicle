// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore;

/// <summary>
/// Represents an implementation of <see cref="IChronicleBuilder"/> for .NET clients.
/// </summary>
/// <param name="services"><see cref="IServiceCollection"/> to use.</param>
/// <param name="configuration"><see cref="IConfiguration"/> to use.</param>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> to use.</param>
public class ChronicleBuilder(IServiceCollection services, IConfiguration configuration, IClientArtifactsProvider clientArtifactsProvider) : IChronicleBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; } = services;

    /// <inheritdoc/>
    public IConfiguration Configuration { get; } = configuration;

    /// <inheritdoc/>
    public IClientArtifactsProvider ClientArtifactsProvider { get; } = clientArtifactsProvider;
}
