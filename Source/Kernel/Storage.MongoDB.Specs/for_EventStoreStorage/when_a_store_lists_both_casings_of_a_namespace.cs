// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.MongoDB.Specs;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;
using KernelSequenceState = Cratis.Chronicle.Storage.EventSequences.EventSequenceState;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

[Collection(MongoDBCollection.Name)]
public class when_a_store_lists_both_casings_of_a_namespace(MongoDBFixture fixture) : Specification
{
    readonly string _prefix = $"combined_case_{Guid.NewGuid():N}_";
    readonly ConcurrentQueue<string> _drops = new();
    MongoClient _client;
    EventStoreDatabase _database;
    EventStoreStorage _store;
    GrainState<NamespacesState> _namespaceState;
    KernelObserverState _observer;
    KernelSequenceState _sequence;
    EventSequenceNumber _tail;
    BsonDocument[] _before;
    BsonDocument[] _after;
    string[] _storedNames;
    string[] _physicalNamespaces;

    async Task Establish()
    {
        var settings = MongoClientSettings.FromConnectionString(fixture.ConnectionString);
        settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
        {
            if (string.Equals(command.CommandName, "dropDatabase", StringComparison.Ordinal) || string.Equals(command.CommandName, "drop", StringComparison.Ordinal)) _drops.Enqueue(command.CommandName);
        });
        _client = new MongoClient(settings);
        var clients = Substitute.For<IMongoDBClientManager>();
        clients.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        _database = new("Studio", clients, Options.Create(new MongoDBOptions { Server = fixture.ConnectionString }), Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix }));
        _store = new(
            "Studio",
            _database,
            Substitute.For<Chronicle.Json.IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            _ => Substitute.For<ISinks>(),
            JobsStorageForSpecs.Create(_client),
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance);

        // The bad list order must not win over the only namespace with its own physical data.
        await _store.Namespaces.Create("cratis", DateTimeOffset.UtcNow.AddDays(-1));
        await _store.Namespaces.Create("Cratis", DateTimeOffset.UtcNow);
        var canonical = _store.GetNamespace("Cratis");
        await canonical.Observers.Save(
            new KernelObserverState
            {
                Identifier = "Studio->Lobby",
                NextEventSequenceNumber = 42UL,
                LastHandledEventSequenceNumber = 41UL,
                RunningState = ObserverRunningState.Active
            },
            EventSequenceId.Outbox);
        await canonical.GetEventSequence(EventSequenceId.Outbox).SaveState(new KernelSequenceState
        {
            SequenceNumber = 42UL,
            TailSequenceNumberPerEventType = new Dictionary<EventTypeId, EventSequenceNumber> { ["source-event"] = 41UL },
            ConstraintIndexWatermark = 40UL,
            MutationEpoch = new(8UL)
        });
        await _database.GetNamespaceDatabase("Cratis").GetEventSequenceCollectionFor(EventSequenceId.Outbox).InsertOneAsync(new(
            41UL,
            CorrelationId.New(),
            [],
            [],
            "source-event",
            DateTimeOffset.UtcNow,
            EventSourceType.Default,
            "source-1",
            EventStreamType.All,
            EventStreamId.Default,
            [],
            new Dictionary<string, BsonDocument> { ["1"] = new("message", "keep-existing-event") },
            new Dictionary<string, string>(),
            []));
        _before = await ReadExistingDocuments();
        _namespaceState = new();
    }

    async Task Because()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("Studio").Returns(_store);
        var provider = new NamespacesStateStorageProvider(storage, NullLogger<NamespacesStateStorageProvider>.Instance);
        await provider.ReadStateAsync("namespaces", GrainId.Create("namespaces", "Studio"), _namespaceState);

        var canonicalName = _namespaceState.State.Namespaces.Single(_ => _.Name != EventStoreNamespaceName.Default).Name;
        var canonical = _store.GetNamespace(canonicalName);
        await canonical.GetEventSequence(EventSequenceId.Outbox).EnsureIndexes();

        // Also exercise #5698's physical-name fallback, not just #5699's repaired logical key.
        var alias = _store.GetNamespace("cratis");
        await alias.GetEventSequence(EventSequenceId.Outbox).EnsureIndexes();
        _observer = await canonical.Observers.Get("Studio->Lobby");
        _sequence = await canonical.GetEventSequence(EventSequenceId.Outbox).GetState();
        _tail = await alias.GetEventSequence(EventSequenceId.Outbox).GetTailSequenceNumber();
        _after = await ReadExistingDocuments();
        _storedNames = (await _store.Namespaces.GetAll()).Select(_ => _.Name.Value).ToArray();
        using var databases = await _client.ListDatabaseNamesAsync();
        _physicalNamespaces = (await databases.ToListAsync()).Where(_ => _.StartsWith($"{_prefix}Studio+es+", StringComparison.Ordinal)).ToArray();
    }

    [Fact] void should_load_one_user_namespace_with_the_existing_casing() => _namespaceState.State.Namespaces.Where(_ => _.Name != EventStoreNamespaceName.Default).Select(_ => _.Name.Value).ShouldContainOnly("Cratis");
    [Fact] void should_delete_only_the_variant_list_entry() => _storedNames.ShouldContainOnly("Cratis", "Default");
    [Fact] void should_keep_only_the_existing_physical_namespace() => _physicalNamespaces.ShouldContainOnly($"{_prefix}Studio+es+Cratis");
    [Fact] void should_never_drop_a_database_or_collection() => _drops.ShouldBeEmpty();
    [Fact] void should_read_the_existing_observer_position() => _observer.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_last_handled_source_event() => _observer.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)41UL);
    [Fact] void should_keep_the_observer_running_state() => _observer.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_read_the_existing_sequence_position() => _sequence.SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_read_the_existing_tail_through_the_alias() => _tail.ShouldEqual((EventSequenceNumber)41UL);
    [Fact] void should_keep_all_observer_sequence_and_event_documents() => _after.Zip(_before).All(_ => _.First.Equals(_.Second)).ShouldBeTrue();

    async Task<BsonDocument[]> ReadExistingDocuments()
    {
        var database = _database.GetNamespaceDatabase("Cratis");
        return
        [
            await database.GetCollection<BsonDocument>(WellKnownCollectionNames.Observers).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync(),
            await database.GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync(),
            await database.GetEventSequenceCollectionAsBsonFor(EventSequenceId.Outbox).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()
        ];
    }

    async Task Destroy()
    {
        using var databases = await _client.ListDatabaseNamesAsync();
        foreach (var name in (await databases.ToListAsync()).Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal)))
        {
            await _client.DropDatabaseAsync(name);
        }
    }
}
