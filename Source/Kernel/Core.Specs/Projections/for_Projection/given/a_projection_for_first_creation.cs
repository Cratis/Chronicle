// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_for_first_creation : Specification
{
    protected static readonly EventType _created = new("Created", EventTypeGeneration.First);
    protected ImmediateProjection _immediate;
    protected ProjectionResult _result;
    protected JsonObject _materialized;
    protected ExpandoObject _eagerSeed;
    protected Exception _error;
    protected virtual JsonObject InitialModelState => new();
    protected virtual bool HasDeclaredValues => false;
    EngineProjection _engine;
    JsonSchema _schema;
    ExpandoObjectConverter _converter;
    IEventSequenceStorage _eventSequenceStorage;
    readonly ObjectComparer _comparer = new();

    async Task Establish()
    {
        _converter = new ExpandoObjectConverter(new TypeFormats());
        _schema = await CreateReadModelSchema();
        _eagerSeed = _converter.ToExpandoObject(InitialModelState, _schema);
        var eventSchema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": { "key": { "type": "string" }, "label": { "type": "string" } } }
            """);
        var storage = CreateStorage(eventSchema);
        var model = new ReadModelDefinition(
            "model",
            "models",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = _schema },
            []);
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(model);
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = CreateDefinition();
        var factory = CreateFactory(storage);
        _engine = await factory.Create("store", EventStoreNamespaceName.Default, stateStorage.State, model, []);
        var projection = await CreateProjection(storage, readModel, stateStorage, factory);
        _immediate = await CreateImmediateProjection(storage, readModel, stateStorage, projection);
    }

    async Task<JsonSchema> CreateReadModelSchema()
    {
        var schema = JsonNode.Parse("""
            { "type": "object", "properties": {
                "id": { "type": "string" }, "name": { "type": "string" },
                "label": { "type": "string" }, "tags": { "type": "array", "items": { "type": "string" } }
            } }
            """)!.AsObject();
        if (HasDeclaredValues)
        {
            schema["properties"]!["value"] = new JsonObject { ["type"] = "integer" };
        }
        return await JsonSchema.FromJsonAsync(schema.ToJsonString());
    }

    Storage.IStorage CreateStorage(JsonSchema eventSchema)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns([CreateEvent(EventSequenceNumber.First)]);
        cursor.MoveNext().Returns(true, false);
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: "key", eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(cursor);
        var storage = Substitute.For<Storage.IStorage>();
        var eventTypeSchema = new EventTypeSchema(_created, EventTypeOwner.Client, EventTypeSource.Code, eventSchema);
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns([eventTypeSchema]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetFor(_created.Id, _created.Generation).Returns(eventTypeSchema);
        storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>()).GetEventSequence(EventSequenceId.Log).Returns(_eventSequenceStorage);
        return storage;
    }

    ProjectionFactory CreateFactory(Storage.IStorage storage)
    {
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        return new(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            _converter,
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance);
    }

    async Task<Projection> CreateProjection(Storage.IStorage storage, IReadModel readModel, IStorage<ProjectionDefinition> stateStorage, IProjectionFactory factory)
    {
        var silo = new TestKitSilo();
        silo.AddService(storage);
        silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        silo.AddService<IObjectComparer>(_comparer);
        silo.AddService(Options.Create(new ChronicleOptions()));
        silo.AddService<ILogger<Projection>>(NullLogger<Projection>.Instance);
        silo.AddService(factory);
        silo.AddProbe(_ => readModel);
        silo.Options.StorageFactory = _ => stateStorage;
        return await silo.CreateGrainAsync<Projection>(new ProjectionKey("projection", "store").ToString());
    }

    async Task<ImmediateProjection> CreateImmediateProjection(Storage.IStorage storage, IReadModel readModel, IStorage<ProjectionDefinition> stateStorage, IProjection projection)
    {
        var silo = new TestKitSilo();
        silo.AddService(storage);
        silo.AddService<IExpandoObjectConverter>(_converter);
        silo.AddService(Substitute.For<IReadModelInstanceVisibilities>());
        silo.AddService<ILogger<ImmediateProjection>>(NullLogger<ImmediateProjection>.Instance);
        silo.AddProbe(_ => projection);
        silo.AddProbe(_ => readModel);
        var sequence = Substitute.For<EventSequences.IEventSequence>();
        sequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.First);
        silo.AddProbe(_ => sequence);
        silo.Options.StorageFactory = _ => stateStorage;
        return await silo.CreateGrainAsync<for_ImmediateProjection.given.TestableImmediateProjection>(
            new ImmediateProjectionKey("projection", "store", EventStoreNamespaceName.Default, EventSequenceId.Log, "key").ToString());
    }

    ProjectionDefinition CreateDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "projection",
        "model",
        false,
        true,
        InitialModelState,
        new Dictionary<EventType, FromDefinition> { [_created] = new(new Dictionary<PropertyPath, string> { ["label"] = "label" }, "key", null) },
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    protected AppendedEvent CreateEvent(EventSequenceNumber number) => AppendedEvent.EmptyWithEventType(_created) with
    {
        Context = AppendedEvent.EmptyWithEventType(_created).Context with { EventSourceId = "key", SequenceNumber = number },
        Content = new { key = "key", label = "Created" }.AsExpandoObject()
    };

    protected async Task Materialize(AppendedEvent @event)
    {
        var sink = Substitute.For<ISink>();
        sink.FindOrDefault(Arg.Any<Key>()).Returns(Task.FromResult<ExpandoObject?>(null));
        var key = ((ResolvedKey)await _engine.GetKeyResolverFor(_created)(_eventSequenceStorage, sink, @event)).Key;
        var context = new ProjectionEventContext(key, @event, new Changeset<AppendedEvent, ExpandoObject>(_comparer, @event, new ExpandoObject()), _engine.GetOperationTypeFor(_created), false);
        context = await new SetInitialState(sink, NullLogger<SetInitialState>.Instance).Perform(_engine, context);
        context = await new HandleEvent(_eventSequenceStorage, sink, NullLogger<HandleEvent>.Instance).Perform(_engine, context);
        var model = InMemoryChanges.Apply(key, context.Changeset.Changes, new ExpandoObject());
        ((IDictionary<string, object?>)model)["id"] = key.Value.ToString();
        ReadModelSubjects.TrackChanges(model, context.Changeset.Changes, @event);
        _materialized = ReadModelSubjects.CopyToJson(model, _converter.ToJsonObject(model, _schema));
    }
}
