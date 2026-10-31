// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage;

[Collection(MongoDBCollection.Name)]
public class when_reading_two_missing_observers(MongoDBFixture fixture) : Specification
{
    IMongoClient _client;
    string _databaseName;
    ObserverStateStorage _storage;
    KernelObserverState _first;
    KernelObserverState _second;

    void Establish()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.GetObserverStateCollection().Returns(database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers));
        _storage = new ObserverStateStorage(namespaceDatabase);
    }

    async Task Because()
    {
        _first = await _storage.Get("first");
        _second = await _storage.Get("second");
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);

    [Fact] void should_not_share_catchup_partitions() => ReferenceEquals(_first.CatchingUpPartitions, _second.CatchingUpPartitions).ShouldBeFalse();
    [Fact] void should_not_share_replaying_partitions() => ReferenceEquals(_first.ReplayingPartitions, _second.ReplayingPartitions).ShouldBeFalse();
    [Fact] void should_not_share_in_flight_partitions() => ReferenceEquals(_first.InFlightPartitions, _second.InFlightPartitions).ShouldBeFalse();
}
