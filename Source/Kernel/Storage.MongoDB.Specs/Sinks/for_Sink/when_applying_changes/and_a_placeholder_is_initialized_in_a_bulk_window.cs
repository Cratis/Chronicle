// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using context = Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes.and_a_placeholder_is_initialized_in_a_bulk_window.context;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes;

[Collection(MongoDBCollection.Name)]
public class and_a_placeholder_is_initialized_in_a_bulk_window(context ctx) : IClassFixture<context>
{
    public class context(MongoDBFixture fixture) : IAsyncLifetime
    {
        readonly string _databaseName = $"chronicle_initialization_specs_{Guid.NewGuid():N}";
        IMongoClient _client = default!;
        Sink _sink = default!;
        IProjection _projection = default!;
        SetInitialState _step = default!;
        ReadModelsCompliance _compliance = default!;

        public IDictionary<string, object?> CatchUpPlaceholder = default!;

        public IDictionary<string, object?> CatchUpCached = default!;
        public IDictionary<string, object?> CatchUpStored = default!;
        public IDictionary<string, object?> ReplayCached = default!;
        public IDictionary<string, object?> ReplayStored = default!;

        public async Task InitializeAsync()
        {
            _client = new MongoClient(fixture.ConnectionString);
            var schema = await JsonSchema.FromJsonAsync("""
                {"type":"object","properties":{"id":{"type":"string"},"status":{"type":"string"},"capacity":{"type":"integer"},"secret":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}
                """);
            var readModel = new ReadModelDefinition("initialization", "Initialization", "initialization", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
            var formats = new TypeFormats();
            var expando = new ExpandoObjectConverter(formats);
            var collections = new SinkCollections(readModel, _client.GetDatabase(_databaseName));
            var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
            _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando, Substitute.For<IReadModelChangeStreams>());
            _step = new SetInitialState(_sink, NullLogger<SetInitialState>.Instance);
            var encryption = new Encryption();
            var keys = new InMemoryEncryptionKeyStorage();
            var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
            _compliance = new ReadModelsCompliance(new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance), new Json.ExpandoObjectConverter(formats));
            _projection = Substitute.For<IProjection>();
            _projection.TargetReadModelSchema.Returns(schema);
            var initial = new ExpandoObject();
            ((IDictionary<string, object?>)initial)["status"] = "open";
            ((IDictionary<string, object?>)initial)["capacity"] = 42;
            _projection.InitialModelState.Returns(initial);

            var key = new Key("shelf", ArrayIndexers.NoIndexers);
            await Apply(key, ProjectionOperationType.ChildrenAffected, 0UL);
            await _sink.BeginBulk();
            await Apply(key, ProjectionOperationType.ChildrenAffected, 1UL);
            CatchUpPlaceholder = (await _sink.FindOrDefault(key))!;
            CatchUpCached = await InitializePlaceholder(key);
            await _sink.EndBulk();
            CatchUpStored = (await _sink.FindOrDefault(key))!;

            var replay = new ReplayContext(new ReadModelType(readModel.Identifier, ReadModelGeneration.First), readModel.ContainerName, "initialization-revert", DateTimeOffset.UtcNow);
            await _sink.BeginReplay(replay);
            ReplayCached = await InitializePlaceholder(key);
            await _sink.EndReplay(replay);
            ReplayStored = (await _sink.FindOrDefault(key))!;
        }

        public async Task DisposeAsync() => await _client.DropDatabaseAsync(_databaseName);

        async Task<IDictionary<string, object?>> InitializePlaceholder(Key key)
        {
            await Apply(key, ProjectionOperationType.ChildrenAffected, 2UL);
            await Apply(key, ProjectionOperationType.From, 3UL);
            return (await _sink.FindOrDefault(key))!;
        }

        async Task Apply(Key key, ProjectionOperationType operation, EventSequenceNumber sequenceNumber)
        {
            var @event = AppendedEvent.EmptyWithEventType(new EventType("shelf-event", EventTypeGeneration.First));
            var context = new ProjectionEventContext(key, @event, new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), @event, new ExpandoObject()), operation, false);
            context = await _step.Perform(_projection, context);
            context = await new DecryptInitialState(_compliance, "store", "namespace").Perform(_projection, context);
            context = await new EncryptChangeset(_compliance, new ObjectComparer(), "store", "namespace").Perform(_projection, context);
            (await _sink.ApplyChanges(key, context.Changeset, sequenceNumber)).ShouldBeEmpty();
        }
    }

    [Fact] void should_keep_the_decrypted_placeholder_uninitialized_in_the_cache() => ctx.CatchUpPlaceholder[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);
    [Fact] void should_not_synthesize_capacity_on_the_decrypted_placeholder() => ctx.CatchUpPlaceholder.ContainsKey("capacity").ShouldBeFalse();
    [Fact] void should_persist_the_configured_capacity_after_catch_up() => ctx.CatchUpStored["capacity"].ShouldEqual(42);
    [Fact] void should_persist_the_configured_capacity_after_replay() => ctx.ReplayStored["capacity"].ShouldEqual(42);
    [Fact] void should_initialize_the_catch_up_cache_before_flushing() => ctx.CatchUpCached[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_persist_the_catch_up_initialization() => ctx.CatchUpStored[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_persist_the_catch_up_initial_values() => ctx.CatchUpStored["status"].ShouldEqual("open");
    [Fact] void should_initialize_the_replay_cache_before_flushing() => ctx.ReplayCached[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_persist_the_replay_initialization() => ctx.ReplayStored[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_persist_the_replay_initial_values() => ctx.ReplayStored["status"].ShouldEqual("open");
}
