// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage;

/// <summary>
/// Verifies ordered partial-write evidence against MongoDB. This spec is CI-only, excluded from the local unit filter.
/// </summary>
/// <param name="fixture">The shared MongoDB fixture.</param>
[Collection(MongoDBCollection.Name)]
public class when_incrementing_and_removing_counts_with_a_rejected_write(MongoDBFixture fixture) : Specification
{
    readonly ObserverId _observerId = "b54d1227-e45b-47c6-ad7f-383316bf13ff";
    readonly KeyValuePair<Key, IReadOnlyDictionary<EventTypeId, EventCount>>[] _counts =
    [
        new((Key)"first", new Dictionary<EventTypeId, EventCount> { ["event"] = 2UL }),
        new((Key)"second", new Dictionary<EventTypeId, EventCount> { ["event"] = 3UL }),
        new((Key)"third", new Dictionary<EventTypeId, EventCount> { ["event"] = 1UL })
    ];
    IMongoClient _client;
    IMongoDatabase _database;
    ObserverHandledCountsStorage _storage;
    Exception _error;
    readonly List<ulong> _totals = [];

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
        await _database.CreateCollectionAsync(WellKnownCollectionNames.ObserverHandledCounts, new CreateCollectionOptions<ObserverPartitionCounts>
        {
            Validator = new BsonDocument("counts.event", new BsonDocument("$lte", 2))
        });
        var eventStoreDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        eventStoreDatabase.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts)
            .Returns(_database.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts));
        _storage = new ObserverHandledCountsStorage(eventStoreDatabase);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Increment(_observerId, _counts));
        await _database.RunCommandAsync<BsonDocument>(new BsonDocument { ["collMod"] = WellKnownCollectionNames.ObserverHandledCounts, ["validator"] = new BsonDocument() });
        await _storage.Increment(_observerId, _counts.Skip(((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount).ToArray());
        foreach (var (partition, _) in _counts)
        {
            _totals.Add((await _storage.GetFor(_observerId, partition))["event"].Value);
        }
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);

    [Fact] void should_confirm_only_the_first_write() => ((ObserverHandledCountsPartiallyIncremented)_error).AppliedCount.ShouldEqual(1);
    [Fact] void should_apply_each_count_once_after_retrying_the_suffix() => _totals.ShouldEqual(new ulong[] { 2, 3, 1 });
}
