// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Seeding;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a default implementation of <see cref="IClientArtifactsProvider"/>.
/// </summary>
/// <remarks>
/// This will use type discovery through the provided <see cref="ICanProvideAssembliesForDiscovery"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="DefaultClientArtifactsProvider"/> class.
/// </remarks>
/// <param name="assembliesProvider"><see cref="ICanProvideAssembliesForDiscovery"/> for discovering types.</param>
public class DefaultClientArtifactsProvider(ICanProvideAssembliesForDiscovery assembliesProvider) : IClientArtifactsProvider
{
    /// <summary>
    ///  The singleton default of the <see cref="DefaultClientArtifactsProvider"/> class with a default assembly provider that includes project and package referenced assemblies.
    /// </summary>
    /// <returns>The default <see cref="DefaultClientArtifactsProvider"/>.</returns>
    public static readonly DefaultClientArtifactsProvider Default = new(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));

    bool _initialized;

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventTypes { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Projections { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ModelBoundProjections { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reactors { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reducers { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ReactorMiddlewares { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForTypesProviders { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForPropertiesProviders { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AdditionalEventInformationProviders { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ConstraintTypes { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueConstraints { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueEventTypeConstraints { get; private set; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventSeeders { get; private set; } = [];

    /// <inheritdoc/>
    public void Initialize()
    {
        if (_initialized) return;

        assembliesProvider.Initialize();
        EventTypes = assembliesProvider.DefinedTypes.Where(_ => _.HasAttribute<EventTypeAttribute>()).ToArray();
        ComplianceForTypesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForType) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ToArray();
        ComplianceForPropertiesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForProperty) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ToArray();
        Projections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IProjectionFor<>))).ToArray();
        ModelBoundProjections = assembliesProvider.DefinedTypes.Where(_ => _.HasModelBoundProjectionAttributes()).ToArray();
        Reactors = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<IReactor>() && !_.IsGenericType).ToArray();
        ReactorMiddlewares = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<IReactorMiddleware>()).ToArray();
        Reducers = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IReducerFor<>)) && !_.IsGenericType).ToArray();
        AdditionalEventInformationProviders = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<ICanProvideAdditionalEventInformation>()).ToArray();
        ConstraintTypes = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(IConstraint) && _.IsAssignableTo(typeof(IConstraint))).ToArray();
        UniqueConstraints = EventTypes.Where(_ => _.GetProperties().Any(p => p.HasAttribute<UniqueAttribute>())).ToArray();
        UniqueEventTypeConstraints = EventTypes.Where(_ => _.HasAttribute<UniqueAttribute>()).ToArray();
        EventSeeders = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<ICanSeedEvents>()).ToArray();

        _initialized = true;
    }
}
