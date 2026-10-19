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

    Task Destroy() => _client.DropDatabaseAsync(_databaseName);
}
