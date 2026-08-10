// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.given;

public class an_immediate_projection : Specification
{
    protected const string EventStore = "the-event-store";
    protected const string Projection = "the-projection";
    protected const string ReadModel = "the-read-model";
    protected const string ReadModelKey = "the-read-model-key";

    protected ImmediateProjection _grain;
    protected TestKitSilo _silo;
    protected IProjection _projection;
    protected IReadModel _readModel;
    protected IEventSequence _eventSequence;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ILogger<ImmediateProjection> _logger;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _projection = Substitute.For<IProjection>();
        _readModel = Substitute.For<IReadModel>();
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _logger = Substitute.For<ILogger<ImmediateProjection>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        var storage = Substitute.For<Storage.IStorage>();
        var eventStoreStorage = Substitute.For<Storage.IEventStoreStorage>();
        var namespaceStorage = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        storage.GetEventStore(EventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(EventStoreNamespaceName.Default).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(_eventSequenceStorage);

        _projection.SubscribeDefinitionsChanged(Arg.Any<INotifyProjectionDefinitionsChanged>()).Returns(Task.CompletedTask);
        _readModel.GetDefinition().Returns(CreateReadModelDefinition());

        _silo.AddService(storage);
        _silo.AddService(_expandoObjectConverter);
        _silo.AddService(_logger);
        _silo.AddProbe(_ => _projection);
        _silo.AddProbe(_ => _readModel);
        _silo.AddProbe(_ => _eventSequence);

        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = CreateProjectionDefinition();
        _silo.Options.StorageFactory = _ => stateStorage;

        var key = new ImmediateProjectionKey(
            Projection,
            EventStore,
            EventStoreNamespaceName.Default,
            EventSequenceId.Log,
            ReadModelKey);
        _grain = await _silo.CreateGrainAsync<TestableImmediateProjection>(key.ToString());
    }

    protected static IEventCursor CreateCursor(params AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        cursor.MoveNext().Returns(true, false);
        return cursor;
    }

    static ProjectionDefinition CreateProjectionDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
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

    static ReadModelDefinition CreateReadModelDefinition() => new(
        ReadModel,
        ReadModel,
        "The read model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        Projection,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
        []);
}
