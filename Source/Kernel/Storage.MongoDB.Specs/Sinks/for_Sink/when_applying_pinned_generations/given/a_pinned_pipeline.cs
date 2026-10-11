// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_pinned_generations.given;

public class a_pinned_pipeline : Specification
{
    protected EventType _first = new("PersonRegistered", 1);
    protected EventType _second = new("PersonRegistered", 2);
    protected ProjectionDefinition _definition;
    protected List<BsonDocument> _rootUpdates = [];
    protected List<BsonDocument> _joinUpdates = [];
    protected List<bool> _upserts = [];
    protected ProjectionPipeline _pipeline;

    ReadModelDefinition _readModel;
    EventTypeSchema[] _schemas;
    IEventSequenceStorage _sequence;
    ProjectionFactory _factory;
    ISink _sink;
    IProjection _projection;
    Dictionary<string, ExpandoObject> _saved = [];

    async Task Establish()
    {
        var formats = new TypeFormats();
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"oldName":{"type":"string"},"newName":{"type":"string"},"joinId":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"oldName":{"type":"string"},"newName":{"type":"string"}}}}}}
            """);
        _readModel = new("model", "model", "model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        var eventSchema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string"},"key":{"type":"string"}}}
            """);
        _schemas = [new(_first, EventTypeOwner.Client, EventTypeSource.Code, eventSchema), new(_second, EventTypeOwner.Client, EventTypeSource.Code, eventSchema)];
        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [_first] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"oldName"] = "name" }, "key", null),
                [_second] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"newName"] = "name" }, "key", null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled)
        { GenerationDelivery = EventGenerationDelivery.Pinned };
        _sequence = Substitute.For<IEventSequenceStorage>();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _factory = new(new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance), values, new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance), new Json.ExpandoObjectConverter(formats), keys, storage, NullLogger<ProjectionFactory>.Instance, eventGenerationRelease: Substitute.For<IEventGenerationRelease>());

        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _rootUpdates.Add(Render(call.Arg<UpdateDefinition<BsonDocument>>()));
            _upserts.Add(call.Arg<UpdateOptions>().IsUpsert);
            return new UpdateResult.Acknowledged(1, 1, null);
        });
        collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _joinUpdates.Add(Render(call.Arg<UpdateDefinition<BsonDocument>>()));
            return new UpdateResult.Acknowledged(0, 0, null);
        });
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(collection);
        var expandoConverter = new ExpandoObjectConverter(formats);
        var mongoConverter = new MongoDBConverter(expandoConverter, formats, _readModel, NullLogger<MongoDBConverter>.Instance);
        var mongoSink = new Sink(_readModel, mongoConverter, collections, new ChangesetConverter(_readModel, mongoConverter, collections, expandoConverter), expandoConverter, Substitute.For<IReadModelChangeStreams>());

        // Replace only reads with a stateful in-memory store. Writes use the real MongoDB sink and converter;
        // the collection records the actual commands, including a zero-row join update.
        _sink = Substitute.For<ISink>();
        _sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>()).Returns(Option<Key>.None());
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(call => _saved.GetValueOrDefault(call.Arg<Key>().ToString()));
        _sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>()).Returns(async call =>
        {
            var key = call.Arg<Key>();
            var changeset = call.Arg<IChangeset<AppendedEvent, ExpandoObject>>();
            var failures = await mongoSink.ApplyChanges(key, changeset, call.Arg<EventSequenceNumber>(), call.Arg<SinkWriteMode>());
            _saved[key.ToString()] = changeset.CurrentState;
            return failures;
        });
    }

    protected async Task CreatePipeline()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        var changes = Substitute.For<IChangesetStorage>();
        ICanPerformProjectionPipelineStep[] steps =
        [
            new ResolveKey(_sequence, _sink, new TypeFormats(), NullLogger<ResolveKey>.Instance),
            new SetInitialState(_sink, NullLogger<SetInitialState>.Instance),
            new HandleEvent(_sequence, _sink, NullLogger<HandleEvent>.Instance),
            new SaveChanges(_sink, changes, true, NullLogger<SaveChanges>.Instance)
        ];
        _pipeline = new(_projection, _sink, changes, new ObjectComparer(), steps, new ProjectionHandleLock(), Substitute.For<IReplayScopedCache>(), NullLogger<ProjectionPipeline>.Instance);
    }

    protected AppendedEvent Event(EventType type, string name)
    {
        dynamic content = new ExpandoObject();
        content.name = name;
        content.key = $"key-{type.Generation.Value}";
        return new(new EventContext(type, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, DateTimeOffset.UtcNow, "store", "namespace", CorrelationId.New(), [], Identity.System, [], EventHash.NotSet), (ExpandoObject)content);
    }

    static BsonDocument Render(UpdateDefinition<BsonDocument> update) => update.Render(new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry)).AsBsonDocument;

    void Destroy() => (_projection as IDisposable)?.Dispose();
}
