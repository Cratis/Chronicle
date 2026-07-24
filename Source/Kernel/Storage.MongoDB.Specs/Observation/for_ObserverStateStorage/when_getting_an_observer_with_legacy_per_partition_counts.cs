// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage;

/// <summary>
/// Proves that an observer-state document written before the per-partition handled-event counts were moved to
/// their own collection is split correctly on first load: the per-partition counts land in the dedicated
/// collection, the observer's running totals are preserved, and the legacy field is cleared so the migration
/// runs only once. Runs against a real MongoDB.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_getting_an_observer_with_legacy_per_partition_counts(MongoDBFixture fixture) : Specification
{
    static readonly ObserverId _observerId = "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
    static readonly EventTypeId _typeA = "a1111111-1111-1111-1111-111111111111";
    static readonly EventTypeId _typeB = "b2222222-2222-2222-2222-222222222222";
    const string PartitionOne = "partition-one";
    const string PartitionTwo = "partition-two";

    static when_getting_an_observer_with_legacy_per_partition_counts()
    {
        BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());
        if (!BsonClassMap.IsClassMapRegistered(typeof(ObserverPartitionCounts)))
        {
            BsonClassMap.RegisterClassMap<ObserverPartitionCounts>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(_ => _.Id);
            });
        }
    }

    IMongoClient _client = default!;
    string _databaseName = default!;
    IMongoDatabase _database = default!;
    IEventStoreNamespaceDatabase _namespaceDatabase = default!;
    ObserverStateStorage _storage = default!;
    ObserverHandledCountsStorage _handledCounts = default!;

    KernelObserverState _result = default!;
    IReadOnlyDictionary<EventTypeId, EventCount> _partitionOneCounts = default!;
    IReadOnlyDictionary<EventTypeId, EventCount> _partitionTwoCounts = default!;
    BsonDocument _observerDocumentAfterMigration = default!;

    async Task Establish()
    {
        _databaseName = $"chronicle_observer_migration_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(_databaseName);

        _namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        _namespaceDatabase.GetObserverStateCollection()
            .Returns(_database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers));
        _namespaceDatabase.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts)
            .Returns(_database.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts));

        _storage = new ObserverStateStorage(_namespaceDatabase);
        _handledCounts = new ObserverHandledCountsStorage(_namespaceDatabase);

        var legacyDocument = new ObserverState
        {
            Id = _observerId,
            RunningState = ObserverRunningState.Active,
            HandledEventCount = 5,
            HandledEventCountPerEventType = new Dictionary<string, ulong>
            {
                { _typeA, 3 },
                { _typeB, 2 }
            },
            HandledEventCountPerPartition = new Dictionary<string, IDictionary<string, ulong>>
            {
                { PartitionOne, new Dictionary<string, ulong> { { _typeA, 2 }, { _typeB, 1 } } },
                { PartitionTwo, new Dictionary<string, ulong> { { _typeA, 1 }, { _typeB, 1 } } }
            }
        };
        await _database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers).InsertOneAsync(legacyDocument);
    }

    async Task Because()
    {
        _result = await _storage.Get(_observerId);
        _partitionOneCounts = await _handledCounts.GetFor(_observerId, PartitionOne);
        _partitionTwoCounts = await _handledCounts.GetFor(_observerId, PartitionTwo);
        _observerDocumentAfterMigration = await _database
            .GetCollection<BsonDocument>(WellKnownCollectionNames.Observers)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", _observerId.Value))
            .FirstAsync();
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    [Fact] void should_preserve_the_running_total() => _result.HandledEventCount.ShouldEqual((EventCount)5UL);

    [Fact] void should_preserve_the_running_per_event_type_total_for_type_a() => _result.HandledEventCountPerEventType[_typeA].ShouldEqual((EventCount)3UL);

    [Fact] void should_preserve_the_running_per_event_type_total_for_type_b() => _result.HandledEventCountPerEventType[_typeB].ShouldEqual((EventCount)2UL);

    [Fact] void should_split_partition_one_type_a_into_the_dedicated_collection() => _partitionOneCounts[_typeA].ShouldEqual((EventCount)2UL);

    [Fact] void should_split_partition_one_type_b_into_the_dedicated_collection() => _partitionOneCounts[_typeB].ShouldEqual((EventCount)1UL);

    [Fact] void should_split_partition_two_type_a_into_the_dedicated_collection() => _partitionTwoCounts[_typeA].ShouldEqual((EventCount)1UL);

    [Fact] void should_split_partition_two_type_b_into_the_dedicated_collection() => _partitionTwoCounts[_typeB].ShouldEqual((EventCount)1UL);

    [Fact] void should_clear_the_legacy_field_from_the_observer_document() => _observerDocumentAfterMigration.Contains(nameof(ObserverState.HandledEventCountPerPartition)).ShouldBeFalse();
}
