// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.Integration;
using Aksio.Cratis.Projections;
using Aksio.Reflection;
using Aksio.Rules;
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
    public IEnumerable<Type> ImmediateProjections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> OutboxProjections => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> Adapters { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> ObserverMiddlewares => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> Rules { get; }

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => throw new NotImplementedException();

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
        Adapters = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IAdapterFor<,>))).ToArray();
        Projections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IProjectionFor<>))).ToArray();
    }
}
