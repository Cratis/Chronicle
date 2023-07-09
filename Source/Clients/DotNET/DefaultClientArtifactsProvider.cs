// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences.Outbox;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Rules;
using Aksio.Reflection;
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
    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Projections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ImmediateProjections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> OutboxProjections { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Adapters { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Observers { get; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientArtifactsProvider"/> class.
    /// </summary>
    /// <param name="assembliesProvider"><see cref="ICanProvideAssembliesForDiscovery"/> for discovering types.</param>
    public DefaultClientArtifactsProvider(ICanProvideAssembliesForDiscovery assembliesProvider)
    {
        EventTypes = assembliesProvider.DefinedTypes.Where(_ => _.HasAttribute<EventTypeAttribute>()).ToArray();
        ComplianceForTypesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForType) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ToArray();
        ComplianceForPropertiesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForProperty) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ToArray();
        Rules = assembliesProvider.DefinedTypes.Where(_ => _.BaseType?.IsGenericType == true && _.BaseType?.GetGenericTypeDefinition() == typeof(RulesFor<,>)).ToArray();
        Adapters = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IAdapterFor<,>)) && !_.IsGenericType).ToArray();
        Projections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IProjectionFor<>))).ToArray();
        ImmediateProjections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IImmediateProjectionFor<>))).ToArray();
        OutboxProjections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IOutboxProjections))).ToArray();
        Observers = assembliesProvider.DefinedTypes.Where(_ => _.HasAttribute<ObserverAttribute>()).ToArray();
        ObserverMiddlewares = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IObserverMiddleware))).ToArray();
        AdditionalEventInformationProviders = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(ICanProvideAdditionalEventInformation))).ToArray();
    }
}
