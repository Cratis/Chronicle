// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Reflection;

namespace Cratis.Chronicle.Orleans.InProcess;

/// <summary>
/// Represents a default implementation of <see cref="IClientArtifactsProvider"/> for Orleans.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultOrleansClientArtifactsProvider"/> class.
/// </remarks>
/// <param name="provider">The base <see cref="IClientArtifactsProvider"/>.</param>
public class DefaultOrleansClientArtifactsProvider(IClientArtifactsProvider provider) : IClientArtifactsProvider
{
    /// <inheritdoc/>
    public IEnumerable<Type> AggregateRootStateTypes { get; } = provider.AggregateRoots
                                            .SelectMany(_ => _.AllBaseAndImplementingTypes())
                                            .Where(_ => _.IsDerivedFromOpenGeneric(typeof(AggregateRoot<>)))
                                            .Select(_ => _.GetGenericArguments()[0])
                                            .ToArray();

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => provider.EventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => provider.Projections;

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors => provider.Reactors;

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers => provider.Reducers;

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares => provider.ReactorMiddlewares;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => provider.ComplianceForTypesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => provider.ComplianceForPropertiesProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> Rules => provider.Rules;

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => provider.AdditionalEventInformationProviders;

    /// <inheritdoc/>
    public IEnumerable<Type> AggregateRoots => provider.AggregateRoots;

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes => provider.ConstraintTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints => provider.UniqueConstraints;

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints => provider.UniqueEventTypeConstraints;

    /// <inheritdoc/>
    public void Initialize() => provider.Initialize();
}
