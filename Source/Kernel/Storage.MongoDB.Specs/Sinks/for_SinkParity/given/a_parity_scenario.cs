// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks.InMemory;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkParity.given;

/// <summary>
/// Base for cross-sink parity scenarios. The same sequence of projected read-model states is
/// driven through both the in-memory sink and a real MongoDB sink, and the resulting documents
/// are compared so that any divergence in how the two sinks apply a changeset fails the spec.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
public abstract class a_parity_scenario(MongoDBFixture fixture) : IAsyncLifetime
{
    readonly ObjectComparer _objectComparer = new();

    IMongoClient _client = default!;
    string _databaseName = default!;
    InMemorySink _inMemorySink = default!;
    Sink _mongoSink = default!;
    ReducerPipeline _inMemoryPipeline = default!;
    ReducerPipeline _mongoPipeline = default!;
    JsonSchema _schema = default!;
    Key _key = default!;

    /// <summary>Gets the read-model type whose schema drives the sinks.</summary>
    protected abstract Type ReadModelType { get; }

    /// <summary>Gets the key value the read model is stored under.</summary>
    protected virtual object KeyValue => "root-1";

    /// <summary>Gets the ordered projected states applied (each factory produces a fresh instance per sink).</summary>
    protected abstract IReadOnlyList<Func<ExpandoObject>> States { get; }

    public ExpandoObject? InMemoryResult { get; private set; }

    public ExpandoObject? MongoResult { get; private set; }

    public IReadOnlyList<PropertyDifference> ParityDifferences { get; private set; } = [];

    /// <summary>Gets a human-readable description of any divergence, or empty when the sinks match.</summary>
    public string ParityReport { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _databaseName = $"chronicle_sink_parity_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        _schema = JsonSchema.FromType(ReadModelType);
        _key = new Key(KeyValue, ArrayIndexers.NoIndexers);

        var typeFormats = new TypeFormats();
        var readModel = CreateReadModelDefinition();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);

        _inMemorySink = new InMemorySink(readModel, typeFormats);

        var collections = new SinkCollections(readModel, database);
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel);
        var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
        _mongoSink = new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter);

        var compliance = new PassthroughReadModelsCompliance();
        _inMemoryPipeline = new ReducerPipeline(readModel, _inMemorySink, _objectComparer, compliance, "test-store", "test-namespace");
        _mongoPipeline = new ReducerPipeline(readModel, _mongoSink, _objectComparer, compliance, "test-store", "test-namespace");

        foreach (var state in States)
        {
            await ApplyThroughBoth(state);
        }

        InMemoryResult = await _inMemorySink.FindOrDefault(_key);
        MongoResult = await _mongoSink.FindOrDefault(_key);

        if (InMemoryResult is not null && MongoResult is not null)
        {
            // Compare the read-model data only. Sink-internal metadata (the '__'-prefixed
            // last-handled-sequence-number and friends) is not part of the projected read model
            // and is legitimately tracked differently by each sink.
            _objectComparer.Compare(WithoutMetadata(InMemoryResult), WithoutMetadata(MongoResult), out var differences);
            ParityDifferences = differences.ToArray();
            ParityReport = string.Join(
                " || ",
                ParityDifferences.Select(_ => $"{_.PropertyPath.Path}: in-memory='{_.Original}' mongo='{_.Changed}'"));
        }
    }

    static ExpandoObject WithoutMetadata(ExpandoObject instance)
    {
        var clone = new ExpandoObject();
        var target = (IDictionary<string, object?>)clone;
        foreach (var (key, value) in (IDictionary<string, object?>)instance)
        {
            if (!key.StartsWith("__", StringComparison.Ordinal))
            {
                target[key] = value;
            }
        }

        return clone;
    }

    public async Task DisposeAsync()
    {
        _inMemorySink?.Dispose();
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    protected static ExpandoObject Expando(params (string Key, object? Value)[] properties)
    {
        var instance = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)instance;
        foreach (var (key, value) in properties)
        {
            dictionary[key] = value;
        }

        return instance;
    }

    async Task ApplyThroughBoth(Func<ExpandoObject> stateFactory)
    {
        await _inMemoryPipeline.Handle(
            new ReducerContext([CreateEvent()], _key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), stateFactory())));

        await _mongoPipeline.Handle(
            new ReducerContext([CreateEvent()], _key),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), stateFactory())));
    }

    ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-parity-read-model",
            "TestParityReadModel",
            $"parity_{Guid.NewGuid():N}",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, _schema }
            },
            []);

    AppendedEvent CreateEvent()
    {
        var context = EventContext.From(
            "test-store",
            "test-namespace",
            EventType.Unknown,
            EventSourceType.Default,
            _key.Value.ToString()!,
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.NotSet);

        return new AppendedEvent(context, new ExpandoObject());
    }

    sealed class PassthroughReadModelsCompliance : IReadModelsCompliance
    {
        public Task<ExpandoObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, ExpandoObject instance) =>
            Task.FromResult(instance);

        public Task<JsonObject> ReleaseJson(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, JsonObject instance) =>
            Task.FromResult(instance);

        public Task<ExpandoObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, ExpandoObject instance) =>
            Task.FromResult(instance);

        public Task<IEnumerable<ExpandoObject>> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, IEnumerable<ExpandoObject> instances) =>
            Task.FromResult(instances);
    }
}
