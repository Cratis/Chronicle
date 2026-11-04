// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.given;

public class a_rebuild_with_erased_subjects : Specification
{
    protected const string Erased = "erased-owner";
    protected const string Live = "live-owner";
    protected static readonly EventType Created = "Created";
    protected static readonly EventType Named = "Named";
    protected readonly InMemoryEncryptionKeyStorage _keys = new();
    protected readonly rebuild_sink _old = new();
    protected readonly rebuild_sink _destination = new();
    protected readonly ObjectComparer _comparer = new();
    protected JsonSchema _schema;
    protected JsonSchemaMetadataManager _manager;
    protected ExpandoObjectConverter _converter;
    protected ReadModelsCompliance _compliance;
    protected EncryptionKeyAtRevision _liveKey;
    protected EncryptionKeyErasure _erasure;
    protected JsonObject _stored;
    protected JsonObject _released;
    protected EngineProjection _engine;
    protected IEventSequenceStorage _events;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "id":{"type":"string"},"key":{"type":"string"},"counter":{"type":"integer"},
              "liveName":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
              "profile":{"type":"object","properties":{
                "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "status":{"type":"string"}}},
              "privateTags":{"type":"array","items":{"type":"string"},"compliance":[{"metadataType":"PII","details":""}]}
            }}
            """);
        _converter = new ExpandoObjectConverter(new TypeFormats());
        var encryption = new Encryption();
        _manager = Manager(_keys, encryption);
        _compliance = new ReadModelsCompliance(_manager, _converter);
        await _keys.SaveFor("store", "Default", Live, encryption.GenerateKey());
        await _keys.SaveFor("store", "Default", Erased, encryption.GenerateKey());
        _liveKey = (await _keys.TryGetLatestFor("store", "Default", Live))!;
        var stale = await _compliance.Apply("store", "Default", _schema, Live, State("stale personal data"));
        ((IDictionary<string, object?>)stale)["obsolete"] = "no longer mapped";
        _old.Documents["document"] = stale;
        _old.Documents["orphan"] = State("orphan personal data");
        await _keys.RecordErasureFor("store", "Default", Erased);
        await _keys.DeleteFor("store", "Default", Erased);
        _erasure = (await _keys.GetErasureFor("store", "Default", Erased))!;
    }

    protected JsonSchemaMetadataManager Manager(IEncryptionKeyStorage keys, IEncryption encryption) =>
        new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);

    protected ExpandoObject State(string name) => _converter.ToExpandoObject(
    new JsonObject
    {
        ["id"] = "document",
        ["key"] = "document",
        ["counter"] = 7,
        ["liveName"] = name,
        ["profile"] = new JsonObject { ["name"] = name, ["status"] = "public" },
        ["privateTags"] = new JsonArray(name)
    },
    _schema);

    protected AppendedEvent Event(EventType type, string subject, ExpandoObject state, ulong sequence = 1) =>
        new(EventContext.From("store", "Default", type, EventSourceType.Default, $"{subject}-source", EventStreamType.All, EventStreamId.Default, sequence, CorrelationId.NotSet) with { Subject = (Subject)subject }, state);

    protected ReadModelDefinition Model(ReadModelObserverType observerType) => new(
        "model",
        "models",
        "Model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        observerType,
        "observer",
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = _schema },
        []);

    protected async Task CreateProjection()
    {
        var storage = Substitute.For<Storage.IStorage>();
        _events = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore((EventStoreName)"store").GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log).Returns(_events);
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            _converter,
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance);
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "model",
            false,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>
            {
                [Created] = new(new Dictionary<PropertyPath, string> { ["key"] = "key", ["counter"] = "counter", ["profile.name"] = "profile.name", ["profile.status"] = "profile.status", ["privateTags"] = "privateTags" }, "key", null),
                [Named] = new(new Dictionary<PropertyPath, string> { ["liveName"] = "liveName" }, "key", null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        _engine = await factory.Create("store", EventStoreNamespaceName.Default, definition, Model(ReadModelObserverType.Projection), []);
    }

    protected async Task ReplayProjection()
    {
        var erased = State(string.Empty);

        // Historical content has already been released by the observer, including whole-PII boundaries.
        ((IDictionary<string, object?>)erased)["privateTags"] = Array.Empty<string>();
        foreach (var @event in new[] { Event(Created, Erased, erased), Event(Named, Live, State("live personal data"), 2) })
        {
            var key = ((ResolvedKey)await _engine.GetKeyResolverFor(@event.Context.EventType)(_events, _destination.Sink, @event)).Key;
            var context = new ProjectionEventContext(key, @event, new Changeset<AppendedEvent, ExpandoObject>(_comparer, @event, new ExpandoObject()), _engine.GetOperationTypeFor(@event.Context.EventType), false);
            context = await new SetInitialState(_destination.Sink, NullLogger<SetInitialState>.Instance).Perform(_engine, context);
            context = await new DecryptInitialState(_compliance, "store", "Default").Perform(_engine, context);
            context = await new HandleEvent(_events, _destination.Sink, NullLogger<HandleEvent>.Instance).Perform(_engine, context);
            context = await new EncryptChangeset(_compliance, _comparer, "store", "Default").Perform(_engine, context);
            await new SaveChanges(_destination.Sink, Substitute.For<IChangesetStorage>(), false, false, NullLogger<SaveChanges>.Instance).Perform(_engine, context);
        }
        await Observe("document");
    }

    protected async Task Reduce(string subject, IReadModelsCompliance? compliance = null)
    {
        var pipeline = new ReducerPipeline(Model(ReadModelObserverType.Reducer), _destination.Sink, _comparer, compliance ?? _compliance, "store", "Default");
        var state = State(subject == Erased ? string.Empty : "live personal data");
        if (subject == Erased) ((IDictionary<string, object?>)state)["privateTags"] = Array.Empty<string>();
        var @event = Event(Created, subject, state);
        await pipeline.Reduce(new ReducerContext([@event], new Concepts.Keys.Key(subject, ArrayIndexers.NoIndexers)), (_, _) => Task.FromResult(
            new ReducerSubscriberResult(new ObserverSubscriberResult(ObserverSubscriberState.Ok, EventSequenceNumber.First, [], string.Empty), state)));
    }

    protected async Task Observe(string id)
    {
        var state = _destination.Documents[id];
        _stored = System.Text.Json.JsonSerializer.SerializeToNode(state)!.AsObject();
        _released = _converter.ToJsonObject(await _compliance.Release("store", "Default", _schema, state), _schema);
    }
}
