// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_projections_manager_grain : Specification
{
    protected const string EventStore = "the-event-store";

    protected ProjectionsManager _grain;
    protected TestKitSilo _silo;
    protected IProjectionsServiceClient _projectionsServiceClient;
    protected IProjectionDefinitionComparer _definitionComparer;
    protected IProjection _projectionGrain;
    protected Observation.IObserver _observerGrain;
    protected ProjectionsManagerState _state;
    protected IEnumerable<ReadModelDefinition> _readModelDefinitions = [];

    async Task Establish()
    {
        _silo = new TestKitSilo();

        _definitionComparer = Substitute.For<IProjectionDefinitionComparer>();
        _silo.AddService(_definitionComparer);

        var engineProjection = Substitute.For<Engine.IProjection>();
        engineProjection.EventTypes.Returns([]);
        engineProjection.IsEventSourceKeyed.Returns(true);
        var projectionFactory = Substitute.For<IProjectionFactory>();
        projectionFactory
            .Create(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<ProjectionDefinition>(),
                Arg.Any<ReadModelDefinition>(),
                Arg.Any<IEnumerable<EventTypeSchema>>())
            .Returns(engineProjection);
        _silo.AddService(projectionFactory);

        _projectionsServiceClient = Substitute.For<IProjectionsServiceClient>();
        _silo.AddService(_projectionsServiceClient);

        _silo.AddService(Substitute.For<ILanguageService>());

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns([]);
        _silo.AddService(storage);

        _silo.AddService(Substitute.For<ILocalSiloDetails>());

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        var readModelsManager = Substitute.For<IReadModelsManager>();
        readModelsManager.GetDefinitions().Returns(_ => Task.FromResult(_readModelDefinitions));
        _silo.AddProbe(_ => readModelsManager);

        _projectionGrain = Substitute.For<IProjection>();
        _silo.AddProbe(_ => _projectionGrain);

        _observerGrain = Substitute.For<Observation.IObserver>();
        _silo.AddProbe(_ => _observerGrain);

        _state = new ProjectionsManagerState();
        var stateStorage = Substitute.For<IStorage<ProjectionsManagerState>>();
        stateStorage.State = _state;
        _silo.Options.StorageFactory = _ => stateStorage;

        _grain = await _silo.CreateGrainAsync<ProjectionsManager>(EventStore);
    }

    protected static ProjectionDefinition CreateDefinition(ProjectionId identifier, ReadModelIdentifier readModel) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        identifier,
        readModel,
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    protected static ReadModelDefinition CreateReadModelDefinition(ReadModelIdentifier identifier) => new(
        identifier,
        "TheReadModel",
        "TheReadModel",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema>(),
        []);
}
