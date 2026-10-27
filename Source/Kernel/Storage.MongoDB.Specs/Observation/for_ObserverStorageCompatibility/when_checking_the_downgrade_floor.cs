// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStorageCompatibility;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_checking_the_downgrade_floor(ReplicaSetMongoDBFixture fixture) : ObserverStorageCompatibilityContract
{
    IMongoClient _client;
    string _databaseName;
    IEventStoreDatabase _database;
    IEventStoreNamespaceDatabase _namespaceDatabase;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        var database = _client.GetDatabase(_databaseName);
        _database = Substitute.For<IEventStoreDatabase>();
        _database.GetCollection<BsonDocument>(Arg.Any<string>()).Returns(call => database.GetCollection<BsonDocument>(call.Arg<string>()));
        _namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        _namespaceDatabase.GetObserverStateCollection().Returns(database.GetCollection<ObserverState>(WellKnownCollectionNames.Observers));
    }

    protected override IEventStoreStorage OpenStore() => new EventStoreStorage(
        "orders",
        _database,
        Substitute.For<Json.IExpandoObjectConverter>(),
        new JsonSerializerOptions(),
        _ => new Chronicle.Storage.Sinks.Sinks("orders", "default", new KnownInstancesOf<ISinkFactory>([])),
        Substitute.For<Orleans.Storage.IJobsStorage>(),
        Options.Create(new ChronicleOptions()),
        NullLoggerFactory.Instance);

    protected override IObserverStateStorage OpenObservers() => new ObserverStateStorage(_namespaceDatabase);

    [Fact] public async Task should_accept_and_stamp_a_legacy_record_without_an_owner()
    {
        var collection = _client.GetDatabase(_databaseName).GetCollection<BsonDocument>(WellKnownCollectionNames.Observers);
        await collection.InsertOneAsync(new BsonDocument { ["_id"] = "legacy" });
        (await OpenObservers().Save(new() { Identifier = "legacy", LastHandledEventSequenceNumber = 42 }, Concepts.EventSequences.EventSequenceId.Inbox)).ShouldBeTrue();
        var stored = await OpenObservers().Get("legacy");
        stored.Owner.ShouldEqual(Concepts.EventSequences.EventSequenceId.Inbox);
        stored.LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)42);
    }

    Task Destroy() => _client.DropDatabaseAsync(_databaseName);
}
