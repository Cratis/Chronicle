// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider.given;

public class an_artifacts_provider : Specification
{
    protected ICanProvideAssembliesForDiscovery _assembliesProvider;
    protected DefaultClientArtifactsProvider _provider;
    protected TypeInfo[] _definedTypes;

    void Establish()
    {
        _definedTypes = [typeof(ClientArtifactDiscovered).GetTypeInfo(), typeof(EventTypeMigration<,>).GetTypeInfo()];
        _assembliesProvider = Substitute.For<ICanProvideAssembliesForDiscovery>();
        _assembliesProvider.DefinedTypes.Returns(_ => _definedTypes);
        _provider = new(_assembliesProvider);
    }

    protected IEnumerable<Type>[] ReadArtifacts() =>
    [
        _provider.EventTypes,
        _provider.Projections,
        _provider.ModelBoundProjections,
        _provider.Reactors,
        _provider.ReadModelReactors,
        _provider.Reducers,
        _provider.ReactorMiddlewares,
        _provider.ComplianceForTypesProviders,
        _provider.ComplianceForPropertiesProviders,
        _provider.AdditionalEventInformationProviders,
        _provider.ConstraintTypes,
        _provider.UniqueConstraints,
        _provider.UniqueEventTypeConstraints,
        _provider.RemoveConstraintEventTypes,
        _provider.EventSeeders,
        _provider.EventTypeMigrators
    ];

    /// <summary>
    /// Represents discovery of a client artifact.
    /// </summary>
    /// <param name="Name">The artifact name.</param>
    [EventType]
    [Unique]
    [RemoveConstraint(nameof(ClientArtifactDiscovered))]
    public record ClientArtifactDiscovered([property: Unique] string Name);

    /// <summary>
    /// Represents discovery of a replacement client artifact during a retry.
    /// </summary>
    [EventType]
    public record ReplacementClientArtifactDiscovered;
}
