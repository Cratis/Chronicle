// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverDefinitionChangeRows;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_persisting_rows(ReplicaSetMongoDBFixture fixture) : DefinitionChangeRowsContract
{
    IMongoClient _client;
    IMongoDatabase _database;
    IEventStoreNamespaceDatabase _namespaceDatabase;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _database = _client.GetDatabase(MongoDBSpecDatabaseNames.New());
        _namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        _namespaceDatabase.GetCollection<BsonDocument>(Arg.Any<string>()).Returns(call => _database.GetCollection<BsonDocument>(call.Arg<string>()));
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);

    protected override IObserverDefinitionChangeRows<T> OpenRows<T>() => new ObserverDefinitionChangeRows<T>(_namespaceDatabase);
}
