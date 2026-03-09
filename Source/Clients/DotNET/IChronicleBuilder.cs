// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle;

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
    /// Gets or sets the client artifacts provider.
    /// </summary>
    IClientArtifactsProvider ClientArtifactsProvider { get; set; }

    /// <summary>
    /// Gets or sets the identity provider.
    /// </summary>
    IIdentityProvider? IdentityProvider { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID accessor.
    /// </summary>
    ICorrelationIdAccessor? CorrelationIdAccessor { get; set; }

    /// <summary>
    /// Gets or sets the event store namespace resolver.
    /// </summary>
    IEventStoreNamespaceResolver? NamespaceResolver { get; set; }
}
