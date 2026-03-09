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
    IEnumerable<Type> _eventTypes = [];
    IEnumerable<Type> _projections = [];
    IEnumerable<Type> _modelBoundProjections = [];
    IEnumerable<Type> _reactors = [];
    IEnumerable<Type> _reducers = [];
    IEnumerable<Type> _reactorMiddlewares = [];
    IEnumerable<Type> _complianceForTypesProviders = [];
    IEnumerable<Type> _complianceForPropertiesProviders = [];
    IEnumerable<Type> _additionalEventInformationProviders = [];
    IEnumerable<Type> _constraintTypes = [];
    IEnumerable<Type> _uniqueConstraints = [];
    IEnumerable<Type> _uniqueEventTypeConstraints = [];
    IEnumerable<Type> _eventSeeders = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventTypes { get { EnsureInitialized(); return _eventTypes; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Projections { get { EnsureInitialized(); return _projections; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ModelBoundProjections { get { EnsureInitialized(); return _modelBoundProjections; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reactors { get { EnsureInitialized(); return _reactors; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reducers { get { EnsureInitialized(); return _reducers; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ReactorMiddlewares { get { EnsureInitialized(); return _reactorMiddlewares; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForTypesProviders { get { EnsureInitialized(); return _complianceForTypesProviders; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForPropertiesProviders { get { EnsureInitialized(); return _complianceForPropertiesProviders; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AdditionalEventInformationProviders { get { EnsureInitialized(); return _additionalEventInformationProviders; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ConstraintTypes { get { EnsureInitialized(); return _constraintTypes; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueConstraints { get { EnsureInitialized(); return _uniqueConstraints; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueEventTypeConstraints { get { EnsureInitialized(); return _uniqueEventTypeConstraints; } }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventSeeders { get { EnsureInitialized(); return _eventSeeders; } }

    /// <summary>
    /// Initializes the provider explicitly.
    /// </summary>
    /// <remarks>
    /// This method is no longer necessary; the provider auto-initializes on first property access.
    /// It is kept for backward compatibility only.
    /// </remarks>
    [Obsolete("Initialize() is no longer needed. The provider auto-initializes on first property access.")]
    public void Initialize() => EnsureInitialized();

    void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        assembliesProvider.Initialize();
        _eventTypes = assembliesProvider.DefinedTypes.Where(_ => _.HasAttribute<EventTypeAttribute>()).ToArray();
        _complianceForTypesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForType) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForType))).ToArray();
        _complianceForPropertiesProviders = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(ICanProvideComplianceMetadataForProperty) && _.IsAssignableTo(typeof(ICanProvideComplianceMetadataForProperty))).ToArray();
        _projections = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IProjectionFor<>))).ToArray();
        _modelBoundProjections = assembliesProvider.DefinedTypes.Where(_ => _.HasModelBoundProjectionAttributes()).ToArray();
        _reactors = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<IReactor>() && !_.IsGenericType).ToArray();
        _reactorMiddlewares = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<IReactorMiddleware>()).ToArray();
        _reducers = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface(typeof(IReducerFor<>)) && !_.IsGenericType).ToArray();
        _additionalEventInformationProviders = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<ICanProvideAdditionalEventInformation>()).ToArray();
        _constraintTypes = assembliesProvider.DefinedTypes.Where(_ => _ != typeof(IConstraint) && _.IsAssignableTo(typeof(IConstraint))).ToArray();
        _uniqueConstraints = _eventTypes.Where(_ => _.GetProperties().Any(p => p.HasAttribute<UniqueAttribute>())).ToArray();
        _uniqueEventTypeConstraints = _eventTypes.Where(_ => _.HasAttribute<UniqueAttribute>()).ToArray();
        _eventSeeders = assembliesProvider.DefinedTypes.Where(_ => _.HasInterface<ICanSeedEvents>()).ToArray();
    }
}
