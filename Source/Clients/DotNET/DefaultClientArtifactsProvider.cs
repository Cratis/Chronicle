// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Types;

namespace Aksio.Cratis;

/// <summary>
/// Represents a default implementation of <see cref="IClientArtifactsProvider"/>.
/// </summary>
/// <remarks>
/// This will use type discovery through the provided <see cref="ICanProvideAssembliesForDiscovery"/>.
/// </remarks>
public class DefaultClientArtifactsProvider : IClientArtifactsProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientArtifactsProvider"/> class.
    /// </summary>
    /// <param name="assembliesProvider"><see cref="ICanProvideAssembliesForDiscovery"/> for discovering types.</param>
    public DefaultClientArtifactsProvider(ICanProvideAssembliesForDiscovery assembliesProvider)
    {
    }

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> ImmediateProjections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> Adapters => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> Rules => throw new NotImplementedException();
}
