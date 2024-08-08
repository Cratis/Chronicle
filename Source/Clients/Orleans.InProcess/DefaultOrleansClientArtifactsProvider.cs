// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;

using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Orleans.InProcess;

/// <summary>
/// Represents a default implementation of <see cref="IClientArtifactsProvider"/> for Orleans.
/// </summary>
public class DefaultOrleansClientArtifactsProvider : DefaultClientArtifactsProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultOrleansClientArtifactsProvider"/> class.
    /// </summary>
    /// <param name="assembliesProvider"><see cref="ICanProvideAssembliesForDiscovery"/> for discovering types.</param>
    public DefaultOrleansClientArtifactsProvider(ICanProvideAssembliesForDiscovery assembliesProvider) : base(assembliesProvider)
    {
#pragma warning disable MA0056 // Don't call a virtual member in the constructor
        AggregateRootStateTypes = AggregateRoots
                                            .SelectMany(_ => _.AllBaseAndImplementingTypes())
                                            .Where(_ => _.IsDerivedFromOpenGeneric(typeof(AggregateRoot<>)))
                                            .Select(_ => _.GetGenericArguments()[0])
                                            .ToArray();
    }

    /// <inheritdoc/>
    public override IEnumerable<Type> AggregateRootStateTypes { get; }
}
