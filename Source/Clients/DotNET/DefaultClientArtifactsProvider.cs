// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Rules;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle;

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
        Reactions = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IReaction)) && !_.IsGenericType).ToArray();
        ReactionMiddlewares = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IReactionMiddleware))).ToArray();
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
    public IEnumerable<Type> Reactions { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ReactionMiddlewares { get; }

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
