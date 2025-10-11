// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore;

/// <summary>
/// Defines a builder for setting up Cratis Chronicle for .NET applications.
/// </summary>
public interface IChronicleBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the client artifacts provider.
    /// </summary>
    IClientArtifactsProvider ClientArtifactsProvider { get; }
}
