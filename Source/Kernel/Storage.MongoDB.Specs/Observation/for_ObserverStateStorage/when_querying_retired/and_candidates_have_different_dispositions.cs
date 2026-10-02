// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.when_querying_retired;

[Collection(MongoDBCollection.Name)]
public class and_candidates_have_different_dispositions(MongoDBFixture fixture) : Specification
{
    IMongoClient _client;
    string _databaseName;
    ObserverStateStorage _storage;
    IEnumerable<ObserverId> _result;

    async Task Establish()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(fixture.ConnectionString);
        var collection = _client.GetDatabase(_databaseName).GetCollection<ObserverState>(WellKnownCollectionNames.Observers);
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetObserverStateCollection().Returns(collection);
        _storage = new(database);
        await collection.InsertManyAsync([
            new ObserverState { Id = "retired", AlertDisposition = AlertDisposition.Retired },
            new ObserverState { Id = "active", AlertDisposition = AlertDisposition.Active },
            new ObserverState { Id = "unsubmitted", AlertDisposition = AlertDisposition.Retired }
        ]);
    }

    async Task Because() => _result = await _storage.GetRetired(["retired", "active", "missing", "retired"]);

    [Fact] void should_only_return_the_submitted_retired_observer() => _result.ShouldContainOnly((ObserverId)"retired");

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
