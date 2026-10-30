// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Namespaces;
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
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection_with_initial_values : Specification
{
    protected static readonly EventType _created = new("Created", EventTypeGeneration.First);
    protected static readonly EventType _updated = new("Updated", EventTypeGeneration.First);
    protected static readonly EventType _removed = new("Removed", EventTypeGeneration.First);
    protected static readonly EventType _childAdded = new("ChildAdded", EventTypeGeneration.First);
    protected Projection _grain;
    protected ExpandoObject _result;
    protected ExpandoObject _materialized;
    EngineProjection _engine;
    IEventSequenceStorage _eventSequenceStorage;
    readonly ObjectComparer _comparer = new();
    protected static IDictionary<string, object?> Properties(ExpandoObject state) => state;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        var storage = CreateStorage();
        var factory = CreateFactory(storage);
        silo.AddService<IProjectionFactory>(factory);
        silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        silo.AddService<IObjectComparer>(_comparer);
        silo.AddService(storage);
        silo.AddService(Options.Create(new ChronicleOptions()));
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([]);
        silo.AddProbe(_ => namespaces);
        var readModel = Substitute.For<IReadModel>();
        var model = await CreateReadModelDefinition();
        readModel.GetDefinition().Returns(model);
        silo.AddProbe(_ => readModel);
        var definition = CreateProjectionDefinition();
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = definition;
        silo.Options.StorageFactory = _ => stateStorage;
        _grain = await silo.CreateGrainAsync<Projection>(new ProjectionKey("projection", "store").ToString());
        _engine = await factory.Create("store", EventStoreNamespaceName.Default, definition, model, []);
    }

    static ProjectionFactory CreateFactory(Storage.IStorage storage)
    {
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        return new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance);
    }

    Storage.IStorage CreateStorage()
    {
        var storage = Substitute.For<Storage.IStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventSequenceStorage.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Option<AppendedEvent>.None());
        _eventSequenceStorage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(EventSequenceNumber.Unavailable);
        _eventSequenceStorage.TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(Option<AppendedEvent>.None())));
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        return storage;
    }

    static async Task<ReadModelDefinition> CreateReadModelDefinition()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            { "type": "object", "properties": {
              "id": { "type": "string" }, "name": { "type": "string" },
              "value": { "type": "integer" }, "children": { "type": "array", "items": {
                "type": "object", "properties": { "id": { "type": "string" } }
              } }
            } }
            """);
        return new ReadModelDefinition(
            "model",
            "models",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
    }

    static ProjectionDefinition CreateProjectionDefinition() => new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "model",
            true,
            true,
            new JsonObject { ["name"] = "Initial", ["value"] = 42 },
            new Dictionary<EventType, FromDefinition>
            {
                [_created] = new(new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId, null)
            },
            new Dictionary<EventType, JoinDefinition>
            {
                [_updated] = new("id", new Dictionary<PropertyPath, string> { ["value"] = "value" }, WellKnownExpressions.EventSourceId)
            },
            new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["children"] = new(
                    "id",
                    new Dictionary<EventType, FromDefinition>
                    {
                        [_childAdded] = new(new Dictionary<PropertyPath, string> { ["id"] = "child" }, "child", "$value(key)")
                    },
                    new Dictionary<EventType, JoinDefinition>(),
                    new Dictionary<PropertyPath, ChildrenDefinition>(),
                    new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
                    new Dictionary<EventType, RemovedWithDefinition>(),
                    new Dictionary<EventType, RemovedWithJoinDefinition>())
            },
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition> { [_removed] = new(WellKnownExpressions.EventSourceId, null) },
            new Dictionary<EventType, RemovedWithJoinDefinition>());

    protected static AppendedEvent Event(EventType type, ulong number, string eventSourceId = "key") => new(
        EventContext.EmptyWithEventSourceId(eventSourceId) with { EventType = type, SequenceNumber = number },
        new { child = "child-1", value = 99 }.AsExpandoObject());

    protected async Task<ExpandoObject> Fold(params AppendedEvent[] events) =>
        (await FoldOnto(SingleReadModelProjectionState.Empty, events)).Model;

    protected static IDictionary<string, object?> WithoutBookkeeping(ExpandoObject state) =>
        Properties(state).Where(_ => !_.Key.StartsWith("__")).ToDictionary(_ => _.Key, _ => _.Value);

    protected async Task<ExpandoObject> Process(params AppendedEvent[] events) =>
        (await _grain.Process(EventStoreNamespaceName.Default, events)).Single();

    protected Task<SingleReadModelProjectionState> FoldOnto(SingleReadModelProjectionState state, params AppendedEvent[] events) =>
        _grain.FoldForSingleReadModel(EventStoreNamespaceName.Default, state, events);

    protected async Task Materialize(params AppendedEvent[] events)
    {
        var sink = Substitute.For<ISink>();
        ExpandoObject state = null;
        sink.FindOrDefault(Arg.Any<Key>()).Returns(_ => Task.FromResult(state?.Clone()));
        sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>()).Returns(Option<Key>.None());
        var initialize = new SetInitialState(sink, NullLogger<SetInitialState>.Instance);
        var handle = new HandleEvent(_eventSequenceStorage, sink, NullLogger<HandleEvent>.Instance);
        foreach (var @event in events)
        {
            var key = ((ResolvedKey)await _engine.GetKeyResolverFor(@event.Context.EventType)(_eventSequenceStorage, sink, @event)).Key;
            var context = new ProjectionEventContext(
                key,
                @event,
                new Changeset<AppendedEvent, ExpandoObject>(_comparer, @event, state ?? new ExpandoObject()),
                _engine.GetOperationTypeFor(@event.Context.EventType),
                false);
            context = await initialize.Perform(_engine, context);
            context = await handle.Perform(_engine, context);
            if (context.Changeset.HasBeenRemoved())
            {
                state = null;
            }
            else if (context.Changeset.HasChanges)
            {
                state = InMemoryChanges.Apply(key, context.Changeset.Changes, state ?? new ExpandoObject());
                Properties(state)["id"] = key.Value.ToString();
                ReadModelSubjects.TrackChanges(state, context.Changeset.Changes, @event);
            }
        }
        _materialized = state;
    }
}
