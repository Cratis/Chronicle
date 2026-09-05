// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Cuts;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;

namespace Cratis.Chronicle.Services.Cuts.for_ReadModelCuts.given;

public class all_dependencies : Specification
{
    protected static readonly EventSequenceId EventSequence = "event-log";
    protected static readonly ReadModelIdentifier ReadModel = "my-read-model";
    protected static readonly ProjectionId Projection = new("my-projection");

    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IReadModelDefinitionsStorage _readModelDefinitionsStorage;
    protected IReadModelCutStorage _cutStorage;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IProjectionsManager _projectionsManager;
    protected IProjection _projection;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ReadModelDefinition _readModelDefinition;
    protected ProjectionDefinition _projectionDefinition;
    protected IEventCursor _cursor;
    protected ContractCuts.IReadModelCuts _service;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _readModelDefinitionsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        _cutStorage = Substitute.For<IReadModelCutStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _projectionsManager = Substitute.For<IProjectionsManager>();
        _projection = Substitute.For<IProjection>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _eventStoreStorage.ReadModels.Returns(_readModelDefinitionsStorage);
        _namespaceStorage.ReadModelCuts.Returns(_cutStorage);
        _namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);

        _grainFactory.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(_projectionsManager);
        _grainFactory.GetGrain<IProjection>(Arg.Any<string>()).Returns(_projection);

        _readModelDefinition = new ReadModelDefinition(
            ReadModel,
            "my-container",
            "My Read Model",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "my-projection",
            new SinkDefinition(SinkConfigurationId.None, SinkTypeId.None),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);
        _readModelDefinitionsStorage.GetAll().Returns([_readModelDefinition]);

        _projectionDefinition = new ProjectionDefinition(
            ProjectionOwner.None,
            EventSequence,
            Projection,
            ReadModel,
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
        _projectionsManager.GetProjectionDefinitions().Returns([_projectionDefinition]);

        _cursor = Substitute.For<IEventCursor>();
        _cursor.MoveNext().Returns(true, false);
        _cursor.Current.Returns([]);
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>()).Returns(_cursor);

        _projection.Process(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns([]);
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());

        _cutStorage.GetManifest(Arg.Any<ReadModelCutId>()).Returns((ReadModelCutManifest?)null);

        _service = new ReadModelCuts(_grainFactory, _storage, _expandoObjectConverter);
    }
}
