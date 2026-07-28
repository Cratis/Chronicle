// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage;

/// <summary>
/// Proves that per-partition handled-event counts accumulate through atomic increments and are removed per
/// partition and per observer, against a real MongoDB.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class when_incrementing_and_removing_counts(MongoDBFixture fixture) : Specification
{
    static readonly ObserverId _observerId = "c7d148a2-6ca5-4bff-8a2f-ffd8534cc812";
    static readonly EventTypeId _typeA = "a1111111-1111-1111-1111-111111111111";
    static readonly EventTypeId _typeB = "b2222222-2222-2222-2222-222222222222";
    static readonly Key _partitionOne = "partition-one";
    static readonly Key _partitionTwo = "partition-two";

    static when_incrementing_and_removing_counts()
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
    ObserverHandledCountsStorage _storage = default!;

    IReadOnlyDictionary<EventTypeId, EventCount> _partitionOneCounts = default!;
    IReadOnlyDictionary<EventTypeId, EventCount> _partitionTwoCounts = default!;
    IReadOnlyDictionary<EventTypeId, EventCount> _partitionOneCountsAfterRemove = default!;
    IReadOnlyDictionary<EventTypeId, EventCount> _partitionTwoCountsAfterRemoveAll = default!;

    void Establish()
    {
        _databaseName = $"chronicle_handled_counts_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);

        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts)
            .Returns(database.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts));

        _storage = new ObserverHandledCountsStorage(namespaceDatabase);
    }

    async Task Because()
    {
        await _storage.Increment(_observerId, _partitionOne, Counts((_typeA, 2), (_typeB, 1)));
        await _storage.Increment(_observerId, _partitionOne, Counts((_typeA, 3)));
        await _storage.Increment(_observerId, _partitionTwo, Counts((_typeA, 1)));

        _partitionOneCounts = await _storage.GetFor(_observerId, _partitionOne);
        _partitionTwoCounts = await _storage.GetFor(_observerId, _partitionTwo);

        await _storage.RemoveFor(_observerId, _partitionOne);
        _partitionOneCountsAfterRemove = await _storage.GetFor(_observerId, _partitionOne);

        await _storage.RemoveAllFor(_observerId);
        _partitionTwoCountsAfterRemoveAll = await _storage.GetFor(_observerId, _partitionTwo);
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    static IReadOnlyDictionary<EventTypeId, EventCount> Counts(params (EventTypeId Type, ulong Count)[] entries) =>
        entries.ToDictionary(_ => _.Type, _ => (EventCount)_.Count);

    [Fact] void should_accumulate_type_a_across_increments() => _partitionOneCounts[_typeA].ShouldEqual((EventCount)5UL);

    [Fact] void should_accumulate_type_b_across_increments() => _partitionOneCounts[_typeB].ShouldEqual((EventCount)1UL);

    [Fact] void should_keep_counts_per_partition_isolated() => _partitionTwoCounts[_typeA].ShouldEqual((EventCount)1UL);

    [Fact] void should_remove_a_single_partition() => _partitionOneCountsAfterRemove.Count.ShouldEqual(0);

    [Fact] void should_remove_all_partitions_for_the_observer() => _partitionTwoCountsAfterRemoveAll.Count.ShouldEqual(0);
}
