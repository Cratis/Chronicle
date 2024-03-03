// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Reflection;
using Aksio.Types;
using Cratis.Aggregates;
using Cratis.Compliance;
using Cratis.Events;
using Cratis.Integration;
using Cratis.Observation;
using Cratis.Projections;
using Cratis.Reducers;
using Cratis.Rules;

namespace Cratis;

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
        assembliesProvider.Initialize();
        EventTypes = assembliesProvider.DefinedTypes.Where(_ => _.HasAttribute<EventTypeAttribute>()).ToArray();
        ComplianceForTypesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForType) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ToArray();
        ComplianceForPropertiesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForProperty) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ToArray();
        Rules = assembliesProvider.DefinedTypes.Where(_ => _.BaseType?.IsGenericType == true && _.BaseType?.GetGenericTypeDefinition() == typeof(RulesFor<,>)).ToArray();
        Adapters = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IAdapterFor<,>)) && !_.IsGenericType).ToArray();
        Projections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IProjectionFor<>))).ToArray();
        ImmediateProjections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IImmediateProjectionFor<>))).ToArray();
        Observers = assembliesProvider.DefinedTypes.Where(_ => _.HasAttribute<ObserverAttribute>()).ToArray();
        ObserverMiddlewares = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IObserverMiddleware))).ToArray();
        Reducers = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IReducerFor<>)) && !_.IsGenericType).ToArray();
        AdditionalEventInformationProviders = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(ICanProvideAdditionalEventInformation))).ToArray();
        AggregateRoots = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IAggregateRoot))).ToArray();
    }

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Projections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ImmediateProjections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Adapters { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Observers { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ObserverMiddlewares { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Rules { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> AggregateRoots { get; }
}
